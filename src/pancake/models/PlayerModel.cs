using Microsoft.Extensions.Logging;
using pancake.lib;
using pancake.spotify;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace pancake.models
{
    public interface IPlayerModel
    {
        event PropertyChangedEventHandler? PropertyChanged;

        bool ClientAvailable { get; }
        bool NeedToken { get; }
        bool EnableControls { get; set; }

        IPlayableItem? CurrentlyPlaying { get; }
        string Title { get; }
        IEnumerable<LinkableObject> Artists { get; }
        int Duration { get; }
        int Position { get; }
        bool IsPlaying { get; }
        bool? IsFavorite { get; set; }
        bool IsShuffleOn { get; }
        string RepeatState { get; }

        Task<bool> PlayPause();
        Task<bool> SkipNext();
        Task<bool> SkipPrevious();
        Task<bool> ToggleFavorite();
        Task<bool> ToggleRepeat();
        Task<bool> ToggleShuffle();

        void SignOut();

        void Dispose();
    }

    public class PlayerModel : IDisposable, INotifyPropertyChanged, IPlayerModel
    {
        private record ChangedState(bool Track = false, bool PlayPause = false, bool Shuffle = false, bool Repeat = false, bool Position = false)
        {
            public static ChangedState AllChanged => new ChangedState(true, true, true, true, true);
            public static ChangedState NothingChanged => new ChangedState();

            public static ChangedState Compare(CurrentlyPlayingContext? oldContext, CurrentlyPlayingContext? newContext)
            {
                if ((oldContext == null || newContext == null) && oldContext != newContext)
                    return ChangedState.AllChanged;
                else if (oldContext == null && newContext == null)
                    return ChangedState.NothingChanged;
                else
                {
                    return new ChangedState(
                        Track: oldContext!.Item?.GetItemId() != newContext!.Item?.GetItemId(),
                        PlayPause: oldContext.IsPlaying != newContext.IsPlaying,
                        Shuffle: oldContext.ShuffleState != newContext.ShuffleState,
                        Repeat: oldContext.RepeatState != newContext.RepeatState,
                        Position: oldContext.ProgressMs > newContext.ProgressMs || (!oldContext.IsPlaying && !newContext.IsPlaying && oldContext.ProgressMs != newContext.ProgressMs));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IConfig _config;
        private readonly IAPI _api;

        private RepeatingRun _statusRefresher;

        private SemaphoreSlim _refreshLock = new SemaphoreSlim(1);
        private readonly int REFRESH_DELAY;
        private readonly System.Threading.Timer _trackTimer;
        private bool _disposed = false;

        private CurrentlyPlayingContext? _context;
        private int _positionMs = 0;
        private bool? _isFavorite = null;
        private bool _enableControls = true;

        private readonly ILogger _stateLog;
        private readonly ILogger _timingLog;
        private readonly ILogger _commandsLog;

        public PlayerModel(IConfig config, IAPI api, ILogging logging)
        {
            _config = config;
            REFRESH_DELAY = _config.RefreshDelayMS;

            _stateLog = logging.Create("pancake.playermodel.state");
            _timingLog = logging.Create("pancake.playermodel.timing");
            _commandsLog = logging.Create("pancake.playermodel.commands");

            _api = api;
            _api.PropertyChanged += _api_PropertyChanged;
            _trackTimer = new Timer(new TimerCallback(_SongTick), this, Timeout.Infinite, Timeout.Infinite);

            _statusRefresher = new RepeatingRun(_RepeatedlyRefreshState, REFRESH_DELAY);
        }


        private CurrentlyPlayingContext? Context { get => _context ?? PrevContext; set => _context = value; }
        private CurrentlyPlayingContext? PrevContext { get; set; }

        public bool NeedToken => !_api.HasToken;

        public string Title
            => Context.GetTrack()?.Name ?? Context.GetEpisode()?.Name ?? "";
        public IEnumerable<LinkableObject> Artists
        {
            get
            {
                var track = Context.GetTrack();
                var episode = Context.GetEpisode();
                if (track != null)
                    return track.Artists.Select(r => (LinkableObject)r);
                else if (episode != null)
                    return new[] { (LinkableObject)episode.Show };
                else
                    return Enumerable.Empty<LinkableObject>();
            }
        }
        public bool? IsFavorite
        {
            get => _isFavorite;
            set
            {
                _isFavorite = value;
                _OnPropertyChanged(nameof(IsFavorite));
            }
        }
        public bool IsPlaying
            => Context?.IsPlaying ?? false;
        public bool IsShuffleOn
            => Context?.ShuffleState ?? false;
        /// <summary>
        /// returns "off", "track", or "context"
        /// </summary>
        public string RepeatState
            => Context?.RepeatState ?? "off";
        public int Position
        {
            get => _positionMs;
            private set
            {
                _positionMs = Math.Max(Math.Min(value, Duration), 0);
                _OnPropertyChanged(nameof(Position));
            }
        }
        public int Duration
            => Context.GetTrack()?.DurationMs ?? Context.GetEpisode()?.DurationMs ?? 0;
        public IPlayableItem? CurrentlyPlaying => Context?.Item;
        public bool ClientAvailable
            => _api.ClientAvailable;

        public bool EnableControls
        {
            get => _enableControls;
            set
            {
                _enableControls = value;
                _OnPropertyChanged(nameof(EnableControls));
            }
        }


        private async void _api_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(API.HasToken))
            {
                _statusRefresher.Stop();

                if (_api.HasToken)
                {
                    await _statusRefresher.Invoke();
                    _statusRefresher.Start();
                }

                _OnPropertyChanged(nameof(NeedToken));
            }
            else if (e.PropertyName == nameof(API.ClientAvailable))
                _OnPropertyChanged(nameof(ClientAvailable));
        }

        public async Task<bool> PlayPause()
        {
            return await _api.TryApiCall(async client =>
            {
                _commandsLog.LogInformation($"play/pause {DateTime.Now.ToString("mm:ss.fff")}");

                var prev = IsPlaying;
                if (IsPlaying)
                    await client!.Player.PausePlayback();
                else
                    await client!.Player.ResumePlayback();

                await RefreshStateUntil(client, r => r.PlayPause);
            });
        }
        public async Task<bool> SkipNext()
        {
            return await _api.TryApiCall(async client =>
            {
                _commandsLog.LogInformation("skip next");

                _statusRefresher.Stop();
                await client!.Player.SkipNext();
                await RefreshStateUntil(client, r => r.Track);
                _statusRefresher.Start();
            });
        }
        public async Task<bool> SkipPrevious()
        {
            return await _api.TryApiCall(async client =>
            {
                if (Position < 3000)
                {
                    _commandsLog.LogInformation("skip prev");

                    await client!.Player.SkipPrevious();
                    await RefreshStateUntil(client, r => r.Track);
                }
                else
                {
                    _commandsLog.LogInformation("skip prev/seek");

                    _statusRefresher.Stop();
                    await client!.Player.SeekTo(new PlayerSeekToRequest(0));
                    await RefreshStateUntil(client, r => r.Position);
                    _statusRefresher.Start();
                }

            });
        }
        public async Task<bool> ToggleShuffle()
        {
            return await _api.TryApiCall(async client =>
            {
                _commandsLog.LogInformation($"toggle shuffle: {IsShuffleOn}");

                await client!.Player.SetShuffle(new PlayerShuffleRequest(!IsShuffleOn));
                await RefreshStateUntil(client, r => r.Shuffle);
            });
        }
        public async Task<bool> ToggleRepeat()
        {
            return await _api.TryApiCall(async client =>
            {
                _commandsLog.LogInformation($"toggle repeat: {RepeatState}");

                PlayerSetRepeatRequest.State nextState;
                switch (RepeatState)
                {
                    case "off":
                        nextState = PlayerSetRepeatRequest.State.Context;
                        break;
                    case "context":
                        nextState = PlayerSetRepeatRequest.State.Track;
                        break;
                    case "track":
                    default:
                        nextState = PlayerSetRepeatRequest.State.Off;
                        break;
                }

                await client!.Player.SetRepeat(new PlayerSetRepeatRequest(nextState));
                await RefreshStateUntil(client, r => r.Repeat);
            });
        }
        public async Task<bool> ToggleFavorite()
        {
            return await _api.TryApiCall(async client =>
            {
                _commandsLog.LogInformation($"toggle favorite: {IsFavorite}");

                string? id = Context?.Item?.GetItemId();

                if (id != null)
                {
                    if (IsFavorite ?? false)
                    {
                        var result = await client!.Library.RemoveTracks(new LibraryRemoveTracksRequest(new string[] { id }));
                        if (result)
                            IsFavorite = false;
                    }
                    else
                    {
                        var result = await client!.Library.SaveTracks(new LibrarySaveTracksRequest(new string[] { id }));
                        if (result)
                            IsFavorite = true;
                    }
                }
            });
        }
        public void SignOut()
        {
            _commandsLog.LogInformation("sign out");

            _statusRefresher.Stop();
            _api.SetToken(null);
        }


        private async Task _RepeatedlyRefreshState(CancellationToken cancelToken)
        {
            await _api.TryApiCall(async client =>
            {
                try
                {
                    await _RefreshState(client, cancelToken);
                }
                catch (APIException e) when
                    (e.Message == "Player command failed: No active device found")
                {
                    _api.ClientAvailable = false;
                }
            });
        }
        private async Task<bool> RefreshStateUntil(ISpotifyClient client, Func<ChangedState, bool> stateChanged)
        {
            await Task.Delay(250);
            for (int i = 0; i < 3; i++)
                if (stateChanged(await _RefreshState(client)))
                    return true;
                else
                {
                    _stateLog.LogWarning("bad refresh");
                    await Task.Delay(250);
                }

            return false;
        }
        private async Task<ChangedState> _RefreshState(ISpotifyClient client, CancellationToken cancelToken = default(CancellationToken))
        {
            if (await _refreshLock.WaitAsync(0))
            {
                _stateLog.LogInformation("enter writer");
                try
                {
                    var newContext = await client!.Player.GetCurrentPlayback(cancelToken);

                    PrevContext = Context ?? PrevContext;
                    Context = newContext;

                    _api.ClientAvailable = newContext != null;

                    var changed = ChangedState.Compare(PrevContext, newContext);

                    if (ClientAvailable)
                    {
                        if (changed.Track)
                        {
                            if (Context?.Item != null)
                            {
                                var isFavs = await client.Library.CheckTracks(new LibraryCheckTracksRequest(new string[] { Context!.Item!.GetItemId()! }));
                                IsFavorite = isFavs.All(r => r);
                            }
                            else
                                IsFavorite = null;
                        }

                        if (Context?.IsPlaying ?? false)
                        {
                            if (REFRESH_DELAY > 1000)
                            {
                                var diff = Math.Abs(Context.ProgressMs - _positionMs);
                                if ((changed.PlayPause && Context.IsPlaying) || changed.Track || diff > 500)
                                {
                                    Position = Context.ProgressMs;

                                    /*
                                     * _positionMs % 1000 = time since the last even second
                                     * (1000 - _positionMs % 1000) = time till the next even second
                                     * (1000 - _positionMs % 1000) + 1000 = a second after that
                                     */

                                    _timingLog.LogInformation($"TICK FIXED | Now: {DateTime.Now.ToString("mm:ss.fff")}, Next tick: {(1000 - _positionMs % 1000)}");
                                    //_timingLog.LogInformation($"correction :{_positionMs.MSasTimeSpan()} {diff.ToString()}");
                                    _trackTimer.Change((1000 - _positionMs % 1000).MSasTimeSpan(), 1000.MSasTimeSpan());
                                }
                                else
                                    _timingLog.LogInformation($"TICK OK | Now: {DateTime.Now.ToString("mm:ss.fff")}, Position :{_positionMs.MSasTimeSpan()}, Diff from Position: {diff}");
                            }
                            else
                                Position = Context!.ProgressMs;
                        }
                        else
                        {
                            _trackTimer.Change(Timeout.Infinite, Timeout.Infinite);
                            Position = Context!.ProgressMs;
                        }
                    }
                    else
                        _trackTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    _OnPropertyChanged("");

                    _stateLog.LogInformation($"Now: {DateTime.Now.ToString("mm:ss.fff")}, {Title}, {String.Join(", ", Artists.Select(r => r.Name))} : {Position.MSasTimeSpan()} / {Duration.MSasTimeSpan()}, IsPlaying: {IsPlaying}");
                    _stateLog.LogInformation(changed.ToString());

                    if (changed.Track)
                        _OnPropertyChanged(nameof(CurrentlyPlaying));

                    return changed;

                }
                finally
                {
                    _refreshLock.Release();
                    _stateLog.LogInformation("exit writer");
                }
            }
            else
            {
                var prev = Context;
                try
                {
                    _stateLog.LogInformation("enter reader");
                    while (!await _refreshLock.WaitAsync(10)) ;
                    return ChangedState.Compare(PrevContext, Context);
                }
                finally
                {
                    _refreshLock.Release();
                    _stateLog.LogInformation("enter reader");
                }
            }
        }


        private async static void _SongTick(object? state)
        {
            if (state is PlayerModel player)
            {
                if (player.Position + 1000 > player.Duration)
                    await player._api.TryApiCall(async client =>
                    {
                        await player._RefreshState(client);
                    });
                else
                {
                    player.Position = player.Position + 1000;
                    player._timingLog.LogInformation($"tick, now: {DateTime.Now.ToString("mm:ss.fff")}, Position:{player.Position.MSasTimeSpan()}");

                }

            }
        }

        private void _OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Dispose()
        {
            if (!_disposed)
            {
                _statusRefresher.Stop();
                _trackTimer.Dispose();

                _disposed = true;
            }
        }
    }
}

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
    public class PlayerModel : IDisposable, INotifyPropertyChanged
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
        public event EventHandler<ApiErrorEventArgs>? ApiError;

        private readonly IConfig _config;
        private readonly IClientFactory _clientFactory;
        private ISpotifyClient? _client = null;
        private Task? _updaterTask = null;
        private CancellationTokenSource? _updaterCancel = null;
        private SemaphoreSlim _refreshLock = new SemaphoreSlim(1);
        private readonly int REFRESH_DELAY;
        private readonly System.Threading.Timer _trackTimer;
        private bool _disposed = false;

        private CurrentlyPlayingContext? _context;
        private int _positionMs = 0;
        private bool? _isFavorite = null;
        private bool _enableControls = true;
        private bool _clientAvailable = true;

        private readonly ILogger _stateLog;
        private readonly ILogger _timingLog;
        private readonly ILogger _commandsLog;

        public PlayerModel(IConfig config, IClientFactory clientFactory, ILogging logging)
        {
            this._config = config;
            REFRESH_DELAY = _config.RefreshDelayMS;

            _stateLog = logging.Create("pancake.playermodel.state");
            _timingLog = logging.Create("pancake.playermodel.timing");
            _commandsLog = logging.Create("pancake.playermodel.commands");

            this._clientFactory = clientFactory;
            _trackTimer = new Timer(new TimerCallback(_SongTick), this, Timeout.Infinite, Timeout.Infinite);
        }

        private CurrentlyPlayingContext? Context { get => _context ?? PrevContext; set => _context = value; }
        private CurrentlyPlayingContext? PrevContext { get; set; }

        private bool _needToken = true;
        public bool NeedToken
        {
            get => _needToken;
            private set
            {
                _needToken = value;
                _OnPropertyChanged(nameof(NeedToken));
            }
        }
        public string Title => Context.GetTrack()?.Name ?? Context.GetEpisode()?.Name ?? "";
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
        public bool IsPlaying => Context?.IsPlaying ?? false;
        public bool IsShuffleOn => Context?.ShuffleState ?? false;
        /// <summary>
        /// returns "off", "track", or "context"
        /// </summary>
        public string RepeatState => Context?.RepeatState ?? "off";
        public int Position
        {
            get => _positionMs;
            private set
            {
                _positionMs = Math.Max(Math.Min(value, Duration), 0);
                _OnPropertyChanged(nameof(Position));
            }
        }
        public int Duration => Context.GetTrack()?.DurationMs ?? Context.GetEpisode()?.DurationMs ?? 0;
        public IPlayableItem? CurrentlyPlaying => Context?.Item;
        public bool ClientAvailable
        {
            get => _clientAvailable;
            private set
            {
                if (_clientAvailable != value)
                {
                    _clientAvailable = value;
                    _OnPropertyChanged(nameof(ClientAvailable));
                }
            }
        }

        public bool EnableControls
        {
            get => _enableControls;
            set
            {
                _enableControls = value;
                _OnPropertyChanged(nameof(EnableControls));
            }
        }

        public void SetToken(object token)
        {
            _StopUpdates();

            _client = _clientFactory.CreateClient(token);
            NeedToken = false;

            _StartUpdates();
        }

        public async Task<bool> PlayPause()
        {
            return await _TryApiCall(async () =>
            {
                _commandsLog.LogInformation($"play/pause {DateTime.Now.ToString("mm:ss.fff")}");

                var prev = this.IsPlaying;
                if (this.IsPlaying)
                    await this._client!.Player.PausePlayback();
                else
                    await this._client!.Player.ResumePlayback();

                await RefreshStateUntil(r => r.PlayPause);
            });
        }
        public async Task<bool> SkipNext()
        {
            return await _TryApiCall(async () =>
            {
                _commandsLog.LogInformation("skip next");

                _StopUpdates();
                await this._client!.Player.SkipNext();
                await RefreshStateUntil(r => r.Track);
                _StartUpdates();
            });
        }
        public async Task<bool> SkipPrevious()
        {
            return await _TryApiCall(async () =>
            {
                if (Position < 3000)
                {
                    _commandsLog.LogInformation("skip prev");

                    await this._client!.Player.SkipPrevious();
                    await RefreshStateUntil(r => r.Track);
                }
                else
                {
                    _commandsLog.LogInformation("skip prev/seek");

                    _StopUpdates();
                    await this._client!.Player.SeekTo(new PlayerSeekToRequest(0));
                    await RefreshStateUntil(r => r.Position);
                    _StartUpdates();
                }
                
            });
        }
        public async Task<bool> ToggleShuffle()
        {
            return await _TryApiCall(async () =>
            {
                _commandsLog.LogInformation($"toggle shuffle: {IsShuffleOn}");

                await this._client!.Player.SetShuffle(new PlayerShuffleRequest(!IsShuffleOn));
                await RefreshStateUntil(r => r.Shuffle);
            });
        }
        public async Task<bool> ToggleRepeat()
        {
            return await _TryApiCall(async () =>
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

                await this._client!.Player.SetRepeat(new PlayerSetRepeatRequest(nextState));
                await RefreshStateUntil(r => r.Repeat);
            });
        }
        public async Task<bool> ToggleFavorite()
        {
            return await _TryApiCall(async () =>
            {
                _commandsLog.LogInformation($"toggle favorite: {IsFavorite}");

                string? id = Context?.Item?.GetItemId();

                if (id != null)
                {
                    if (IsFavorite ?? false)
                    {
                        var result = await this._client!.Library.RemoveTracks(new LibraryRemoveTracksRequest(new string[] { id }));
                        if (result)
                            this.IsFavorite = false;
                    }
                    else
                    {
                        var result = await this._client!.Library.SaveTracks(new LibrarySaveTracksRequest(new string[] { id }));
                        if (result)
                            this.IsFavorite = true;
                    }
                }
            });
        }
        public void SignOut()
        {
            _commandsLog.LogInformation("sign out");

            _StopUpdates();
            NeedToken = true;
        }


        #region Status Updater
        private void _StartUpdates()
        {
            this._StopUpdates();

            this._updaterCancel = new CancellationTokenSource();
            this._updaterTask = Task.Run(_Updater, this._updaterCancel.Token);
        }
        private void _StopUpdates()
        {
            if (this._updaterCancel != null)
            {
                this._updaterCancel.Cancel();
                this._updaterCancel.Token.WaitHandle.WaitOne(1000);
                this._updaterCancel.Dispose();
                this._updaterCancel = null;
            }

            if (this._updaterTask != null)
            {
                if (this._updaterTask.Status != TaskStatus.WaitingForActivation)
                    this._updaterTask.Dispose();
                this._updaterTask = null;
            }
        }
        private async Task _Updater()
        {
            await _TryApiCall(async () =>
            {
                var cancelToken = this._updaterCancel!.Token;
                var timer = new PeriodicTimer(REFRESH_DELAY.MSasTimeSpan());
                while (!cancelToken.IsCancellationRequested)
                {
                    try
                    {
                        await _RefreshState(cancelToken);

                        await timer.WaitForNextTickAsync(cancelToken);
                    }
                    catch (APIException e) when (e.Message == "Service unavailable")
                    {
                        await Task.Delay(60_000);
                    }
                    catch (APIException e) when
                        (e.Message == "Player command failed: No active device found")
                    {
                        ClientAvailable = false;
                    }
                }
            });
        }

        private async Task<bool> RefreshStateUntil(Func<ChangedState, bool> stateChanged)
        {
            await Task.Delay(250);
            for (int i = 0; i < 3; i++)
                if (stateChanged(await _RefreshState()))
                    return true;
                else
                {
                    _stateLog.LogWarning("bad refresh");
                    await Task.Delay(250);
                }

            return false;
        }
        
        private async Task<ChangedState> _RefreshState(CancellationToken cancelToken = default(CancellationToken))
        {
            if (await _refreshLock.WaitAsync(0))
            {
                _stateLog.LogInformation("enter writer");
                try
                {
                    var newContext = await this._client!.Player.GetCurrentPlayback(cancelToken);

                    PrevContext = Context ?? PrevContext;
                    Context = newContext;

                    ClientAvailable = newContext != null;
                    
                    var changed = ChangedState.Compare(PrevContext, newContext);

                    if (ClientAvailable)
                    {
                        if (changed.Track)
                        {
                            if (Context?.Item != null)
                            {
                                var isFavs = await this._client.Library.CheckTracks(new LibraryCheckTracksRequest(new string[] { Context!.Item!.GetItemId()! }));
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
                                    this.Position = Context.ProgressMs;

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
                                this.Position = Context!.ProgressMs;
                        }
                        else
                        {
                            _trackTimer.Change(Timeout.Infinite, Timeout.Infinite);
                            this.Position = Context!.ProgressMs;
                        }
                    }
                    else
                        _trackTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    _OnPropertyChanged("");

                    _stateLog.LogInformation($"Now: {DateTime.Now.ToString("mm:ss.fff")}, {Title}, {String.Join(", ", Artists.Select(r => r.Name))} : {Position.MSasTimeSpan()} / {Duration.MSasTimeSpan()}, IsPlaying: {IsPlaying}");
                    _stateLog.LogInformation(changed.ToString());
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

        private static void _SongTick(object? state)
        {
            if (state is PlayerModel player)
            {
                if (player.Position + 1000 > player.Duration)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    player._RefreshState();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                else
                {
                    player.Position = player.Position + 1000;
                    player._timingLog.LogInformation($"tick, now: {DateTime.Now.ToString("mm:ss.fff")}, Position:{player.Position.MSasTimeSpan()}");

                }

            }
        }

        #endregion

        private async Task<bool> _TryApiCall(Func<Task> a)
        {
            try
            {
                int retriesLeft = 3;
                while (retriesLeft > 0)
                {
                    try
                    {
                        await a();

                        ClientAvailable = true;

                        return true;
                    }
                    catch (APITooManyRequestsException e)
                    {
                        await Task.Delay(e.RetryAfter);
                    }
                    catch (APIException e) when (e.Message == "Player command failed: Restriction violated")
                    {
                        //retry
                    }
                    catch (APIException e) when
                        (e.Message == "Service unavailable")
                    {
                        //fail silently
                        break;
                    }
                    catch (APIException e) when
                        (e.Message == "Player command failed: No active device found")
                    {
                        ClientAvailable = false;
                        break;
                    }
                    catch (HttpRequestException e) when
                        (e.Message == "No such host is known. (api.spotify.com:443)")
                    {
                        //should we have a no connection state?
                        break;
                    }
                    /* the client blew up on some ssl exception at some point, 
                     * but I didn't record the exception type. This is where it
                     * should be handled. */
                    //catch(HttpException) 
                    //{

                    //}

                    retriesLeft--;
                }

                return false;
            }
            catch (APIException e) when (e is not APITooManyRequestsException)
            {
                //this._dispatcher.Invoke(() => _HandleAPIError(e));
                _HandleAPIError(e);
                return false;
            }
        }
        private void _HandleAPIError(Exception e)
        {
            _OnApiError(e);
        }

        private void _OnPropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private void _OnApiError(Exception e) =>
            this.ApiError?.Invoke(this, new ApiErrorEventArgs(e));

        public void Dispose()
        {
            if (!_disposed)
            {
                this._StopUpdates();
                this._trackTimer.Dispose();

                _disposed = true;
            }
        }
    }
}

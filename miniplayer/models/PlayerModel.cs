using miniplayer.lib;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
//using System.Timers;
using System.Windows.Threading;

namespace miniplayer.models
{
    //internal enum RepeatState
    //{
    //    off,
    //    track,
    //    context
    //}
    internal class PlayerModel : IDisposable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<ApiErrorEventArgs>? ApiError;

        private readonly Dispatcher _dispatcher;

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
        private SpotifyClient? _client = null;
        private Task? _updaterTask = null;
        private CancellationTokenSource? _updaterCancel = null;
        private SemaphoreSlim _refreshLock = new SemaphoreSlim(1);
        private readonly int REFRESH_DELAY = Config.RefreshDelayMS;
        private readonly System.Threading.Timer _trackTimer;
        private bool _disposed = false;

        private CurrentlyPlayingContext? _context;
        private int _positionMs = 0;
        private bool? _isFavorite = null;

        public PlayerModel(Dispatcher dispatcher)
        {
            this._dispatcher = dispatcher;
            _trackTimer = new Timer(new TimerCallback(_SongTick), this, Timeout.Infinite, Timeout.Infinite);
        }

        public void SetToken(IRefreshableToken token)
        {
            this._StopUpdates();

            var authenticator = Authentication.CreateAuthenticator(token);

            var config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(authenticator);

            this._client = new SpotifyClient(config);
            NeedToken = false;

            this._StartUpdates();
        }

        public string Title => _context.GetTrack()?.Name ?? _context.GetEpisode()?.Name ?? "";
        public string Artist
        {
            get
            {
                var track = _context.GetTrack();
                var episode = _context.GetEpisode();
                if (track != null)
                    return string.Join(", ", track.Artists.Select(r => r.Name));
                else if (episode != null)
                    return episode.Show.Name;
                else
                    return "";
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
        public bool IsPlaying => _context?.IsPlaying ?? false;
        public bool IsShuffleOn => _context?.ShuffleState ?? false;
        /// <summary>
        /// returns "off", "track", or "context"
        /// </summary>
        public string RepeatState => _context?.RepeatState ?? "off";
        public int Position
        {
            get => _positionMs;
            private set
            {
                _positionMs = Math.Max(Math.Min(value, Duration), 0);
                _OnPropertyChanged(nameof(Position));
            }
        }
        public int Duration => _context.GetTrack()?.DurationMs ?? _context.GetEpisode()?.DurationMs ?? 0;


        public async Task<bool> PlayPause()
        {
            return await _TryCatchApiCalls(async () =>
            {
                if (this.IsPlaying)
                    await this._client!.Player.PausePlayback();
                else
                    await this._client!.Player.ResumePlayback();

                await _RefreshState();
            });
        }
        public async Task<bool> SkipNext()
        {
            return await _TryCatchApiCalls(async () =>
            {
                _StopUpdates();
                await this._client!.Player.SkipNext();
                await Task.Delay(250);
                for (int i = 0; i < 3; i++)
                    if (await _RefreshState())
                        break;
                    else
                    {
                        Debug.WriteLine("bad refresh");
                        await Task.Delay(250);
                    }
                _StartUpdates();
            });
        }
        public async Task<bool> SkipPrevious()
        {
            return await _TryCatchApiCalls(async () =>
            {
                if (Position < 3000)
                    await this._client!.Player.SkipPrevious();
                else
                    await this._client!.Player.SeekTo(new PlayerSeekToRequest(0));
                await _RefreshState();
            });
        }
        public async Task<bool> ToggleShuffle()
        {
            return await _TryCatchApiCalls(async () =>
            {
                await this._client!.Player.SetShuffle(new PlayerShuffleRequest(!IsShuffleOn));
                await _RefreshState();
            });
        }
        public async Task<bool> ToggleRepeat()
        {
            return await _TryCatchApiCalls(async () =>
            {
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
                await _RefreshState();
            });
        }
        public async Task<bool> ToggleFavorite()
        {
            return await _TryCatchApiCalls(async () =>
            {
                string? id = _context?.Item?.GetItemId();

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
            await _TryCatchApiCalls(async () =>
            {
                var cancelToken = this._updaterCancel!.Token;
                var timer = new PeriodicTimer(new TimeSpan(0, 0, 0, 0, REFRESH_DELAY));
                while (!cancelToken.IsCancellationRequested)
                {
                    await _RefreshState(cancelToken);

                    await timer.WaitForNextTickAsync(cancelToken);
                }
            });
        }
        
        private async Task<bool> _RefreshState(CancellationToken cancelToken = default(CancellationToken))
        {
            //if (await _refreshLock.WaitAsync(0))
            //{
                try
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var newContext = await this._client!.Player.GetCurrentPlayback(cancelToken);
                    var diffSong = this._dispatcher.Invoke(() => this._SetContext(newContext));

                    if (diffSong)
                    {
                        this._dispatcher.Invoke(() => this.IsFavorite = null);
                        var isFavs = await this._client.Library.CheckTracks(new LibraryCheckTracksRequest(new string[] { newContext!.Item!.GetItemId()! }));
                        this.IsFavorite = isFavs.All(r => r);
                    }

                    sw.Stop();
                    
                    if (_context?.IsPlaying ?? false)
                        _trackTimer.Change((_positionMs + sw.ElapsedMilliseconds) % 1000 + 1000, 1000);
                    else
                        _trackTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    return diffSong;

                }
                finally
                {
                    //_refreshLock.Release();
                }
            //}
            //else
            //    return false;
        }
        private static void _SongTick(object? state)
        {
            if (state is PlayerModel player)
            {
                if (player.Position + 1000 > player.Duration)
                    player._RefreshState();
                else
                    player.Position += 1000;

            }
        }

        #endregion


        private bool _SetContext(CurrentlyPlayingContext context)
        {
            var diffSong = context?.Item != null && _context?.Item?.GetItemId() != context?.Item?.GetItemId();

            _context = context;
            _positionMs = context?.ProgressMs ?? 0;
            _OnPropertyChanged("");

            return diffSong;
        }
        private async Task<bool> _TryCatchApiCalls(Func<Task> a)
        {
            try
            {
                await a();
                return true;
            }
            catch (APIException e) when (e is not APITooManyRequestsException)
            {
                this._dispatcher.Invoke(() => _HandleAPIError(e));
                return false;
            }
        }
        private void _HandleAPIError(Exception e)
        {
            _StopUpdates();
            _dispatcher.Invoke(() => this.NeedToken = true);

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

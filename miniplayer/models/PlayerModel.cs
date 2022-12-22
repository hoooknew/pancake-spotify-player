using miniplayer.lib;
using SpotifyAPI.Web;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace miniplayer.models
{
    internal class PlayerModel : IDisposable, INotifyPropertyChanged
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

        private readonly Dispatcher _dispatcher;


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
        private bool _enableControls = true;

        public PlayerModel(Dispatcher dispatcher)
        {
            this._dispatcher = dispatcher;
            _trackTimer = new Timer(new TimerCallback(_SongTick), this, Timeout.Infinite, Timeout.Infinite);
        }

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

        public bool EnableControls
        {
            get => _enableControls;
            set
            {
                _enableControls = value;
                _OnPropertyChanged(nameof(EnableControls));
            }
        }


        public void SetToken(IRefreshableToken token)
        {
            _StopUpdates();

            var authenticator = Authentication.CreateAuthenticator(token);

            var config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(authenticator);

            _client = new SpotifyClient(config);
            NeedToken = false;

            _StartUpdates();
        }

        public async Task<bool> PlayPause()
        {
            return await _TryApiCall(async () =>
            {
                if (this.IsPlaying)
                    await this._client!.Player.PausePlayback();
                else
                    await this._client!.Player.ResumePlayback();

                await RefreshStateUtil(r => r.PlayPause);
            });
        }
        public async Task<bool> SkipNext()
        {
            return await _TryApiCall(async () =>
            {
                _StopUpdates();
                await this._client!.Player.SkipNext();
                await RefreshStateUtil(r => r.Track);
                _StartUpdates();
            });
        }
        public async Task<bool> SkipPrevious()
        {
            return await _TryApiCall(async () =>
            {
                if (Position < 3000)
                {
                    await this._client!.Player.SkipPrevious();
                    await RefreshStateUtil(r => r.Track);
                }
                else
                {
                    _StopUpdates();
                    await this._client!.Player.SeekTo(new PlayerSeekToRequest(0));
                    await RefreshStateUtil(r => r.Position);
                    _StartUpdates();
                }
            });
        }
        public async Task<bool> ToggleShuffle()
        {
            return await _TryApiCall(async () =>
            {
                await this._client!.Player.SetShuffle(new PlayerShuffleRequest(!IsShuffleOn));
                await RefreshStateUtil(r => r.Shuffle);
            });
        }
        public async Task<bool> ToggleRepeat()
        {
            return await _TryApiCall(async () =>
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
                await RefreshStateUtil(r => r.Repeat);
            });
        }
        public async Task<bool> ToggleFavorite()
        {
            return await _TryApiCall(async () =>
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
            await _TryApiCall(async () =>
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

        private async Task<bool> RefreshStateUtil(Func<ChangedState, bool> stateChanged)
        {
            await Task.Delay(250);
            for (int i = 0; i < 3; i++)
                if (stateChanged(await _RefreshState()))
                    return true;
                else
                {
                    Debug.WriteLine("bad refresh");
                    await Task.Delay(250);
                }

            return false;
        }
        private async Task<ChangedState> _RefreshState(CancellationToken cancelToken = default(CancellationToken))
        {
            if (await _refreshLock.WaitAsync(0))
            {
                try
                {
                    var oldContext = _context;
                    _context = await this._client!.Player.GetCurrentPlayback(cancelToken);

                    var changed = ChangedState.Compare(oldContext, _context);

                    if (changed.Track)
                    {
                        IsFavorite = null;
                        if (_context?.Item != null)
                        {
                            var isFavs = await this._client.Library.CheckTracks(new LibraryCheckTracksRequest(new string[] { _context!.Item!.GetItemId()! }));
                            IsFavorite = isFavs.All(r => r);
                        }
                    }

                    if (_context != null && REFRESH_DELAY > 1000 && (_context?.IsPlaying ?? false))
                    {
                        if (changed.Track || Math.Abs(_context.ProgressMs - _positionMs) > 500)
                        {
                            this.Position = _context.ProgressMs;

                            /*
                             * _positionMs % 1000 = time since the last even second
                             * (1000 - _positionMs % 1000) = time till the next even second
                             * (1000 - _positionMs % 1000) + 1000 = a second after that
                             */
                            _trackTimer.Change((1000 - _positionMs % 1000) + 1000, 1000);
                        }
                    }
                    else
                        _trackTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    _OnPropertyChanged("");

                    Debug.WriteLine(changed);
                    return changed;

                }
                finally
                {
                    _refreshLock.Release();
                }
            }
            else
            {
                Debug.WriteLine("skipped refresh because of lock.");
                return ChangedState.NothingChanged;
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
                    player.Position = player.Position + 1000;

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
                        return true;
                    }
                    catch (APITooManyRequestsException e)
                    {
                        await Task.Delay(e.RetryAfter);
                    }
                    catch (APIException e) when (e.Message == "Player command failed: Restriction violated")
                    {
                    }
                    catch(APIException e) when (e.Message == "Player command failed: No active device found")
                    {
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

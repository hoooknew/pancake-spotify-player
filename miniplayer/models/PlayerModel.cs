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

        public PlayerModel(Dispatcher dispatcher)
        {
            this._dispatcher = dispatcher;
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

        private async Task _RefreshState(CancellationToken cancelToken = default(CancellationToken))
        {
            if (await _refreshLock.WaitAsync(0))
            {
                try
                {
                    var newContext = await this._client!.Player.GetCurrentPlayback(cancelToken);
                    var oldContext = this._dispatcher.Invoke(() => this._context);
                    this._dispatcher.Invoke(() => this._SetContext(newContext));

                    if (newContext?.Item != null && oldContext?.Item?.GetItemId() != newContext?.Item?.GetItemId())
                    {
                        this._dispatcher.Invoke(() => this.IsFavorite = null);
                        var isFavs = await this._client.Library.CheckTracks(new LibraryCheckTracksRequest(new string[] { newContext!.Item!.GetItemId()! }));
                        this.IsFavorite = isFavs.All(r => r);
                    }

                }
                finally
                {
                    _refreshLock.Release();
                }
            }
        }
        #endregion



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
                await this._client!.Player.SkipNext();
                await _RefreshState();
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

        private CurrentlyPlayingContext? _context;
        private FullTrack? _Track => _context?.Item as FullTrack;
        private FullEpisode? _Episode => _context?.Item as FullEpisode;
        public string Title => _Track?.Name ?? _Episode?.Name ?? "";
        public string Artist
        {
            get
            {
                if (_Track != null)
                    return string.Join(", ", _Track!.Artists.Select(r => r.Name));
                else if (_Episode != null)
                    return _Episode!.Show.Name;
                else
                    return "";
            }
        }

        private bool? _isFavorite = null;
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

        private int _positionMs = 0;
        public int Position
        {
            get => _positionMs;
            set
            {
                _positionMs = Math.Max(Math.Min(value, Duration), 0);
                _OnPropertyChanged(nameof(Position));
            }
        }
        public int Duration => _Track?.DurationMs ?? _Episode?.DurationMs ?? 0;

        private void _SetContext(CurrentlyPlayingContext context)
        {
            _context = context;
            _positionMs = context?.ProgressMs ?? 0;
            _OnPropertyChanged("");
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
            this._StopUpdates();
        }
    }
}

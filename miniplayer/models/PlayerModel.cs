using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Timers;
using System.Windows.Threading;

namespace miniplayer.models
{
    internal enum RepeatState
    {
        off,
        track,
        context
    }
    internal class PlayerModel : IDisposable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;        
        
        private readonly Dispatcher _dispatcher;
        private readonly SpotifyClient _client;
        private readonly Task _stateUpdaterTask;
        private readonly CancellationTokenSource _stateUpdaterCTS;

        public PlayerModel(SpotifyClient client, Dispatcher dispatcher) 
        {            
            this._client = client;
            this._dispatcher = dispatcher;

            this._stateUpdaterCTS = new CancellationTokenSource();
            this._stateUpdaterTask = Task.Run(_StateUpdater, this._stateUpdaterCTS.Token);
        }

        private async Task _StateUpdater()
        {
            var cancelToken = this._stateUpdaterCTS.Token;
            var timer = new PeriodicTimer(new TimeSpan(0, 0, 0, 0, 500));
            while (!cancelToken.IsCancellationRequested)
            {
                var newContext = await this._client.Player.GetCurrentPlayback(cancelToken);
                var oldContext = this._dispatcher.Invoke(() => this._context);
                this._dispatcher.Invoke(() => this.SetContext(newContext));

                //if (newContext?.Item is FullTrack track)
                //{
                //    Debug.WriteLine($"{track.Name} {TimeSpan.FromMilliseconds(newContext.ProgressMs)}");
                //}

                await timer.WaitForNextTickAsync(cancelToken);
            }
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
                OnPropertyChanged(nameof(IsFavorite));
            }
        }

        public bool IsPlaying => _context?.IsPlaying ?? false;
        public bool IsShuffleOn => _context?.ShuffleState ?? false;
        public RepeatState RepeatState => Enum.Parse<RepeatState>(_context?.RepeatState ?? nameof(RepeatState.off));

        private int _positionMs = 0;
        public int Position
        {
            get => _positionMs;
            set
            {
                _positionMs = Math.Max(Math.Min(value, Duration), 0);
                OnPropertyChanged(nameof(Position));
            }
        }
        public int Duration => _Track?.DurationMs ?? _Episode?.DurationMs ?? 0;

        internal void SetContext(CurrentlyPlayingContext context)
        {
            _context = context;
            _isFavorite = null;
            _positionMs = context?.ProgressMs ?? 0;           
            OnPropertyChanged("");
        }

        private void OnPropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Dispose()
        {
            this._stateUpdaterCTS.Cancel();
            this._stateUpdaterCTS.Token.WaitHandle.WaitOne(1000);
            this._stateUpdaterCTS.Dispose();
            this._stateUpdaterTask.Dispose();
        }
    }
}

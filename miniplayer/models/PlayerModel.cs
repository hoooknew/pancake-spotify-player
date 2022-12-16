using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniplayer.models
{
    internal enum RepeatState
    {
        off,
        track,
        context
    }
    internal class PlayerModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

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
        public bool ShuffleState => _context?.ShuffleState ?? false;
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

    }
}

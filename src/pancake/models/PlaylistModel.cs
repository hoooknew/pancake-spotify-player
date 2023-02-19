using pancake.lib;
using pancake.spotify;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.models
{
    internal class PlaylistModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IPlayerModel _playerModel;
        public IPlayableItem? _playing;

        public ObservableCollection<IPlayableItem> Played { get; private set; }
        public IPlayableItem? Playing 
        { 
            get => _playing; 
            private set
            {
                _playing= value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Playing)));
            }
        }
        public ObservableCollection<IPlayableItem> Queued { get; private set; }

        public PlaylistModel(IPlayerModel playerModel, IConfig config, IClientFactory clientFactory, ILogging logging)
        {
            _playerModel = playerModel;
            _playerModel.PropertyChanged += PlayerModel_PropertyChanged;

            //clientFactory.

            Played = new ObservableCollection<IPlayableItem>();
            Queued = new ObservableCollection<IPlayableItem>();
            Playing = null;
        }

        private void PlayerModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(PlayerModel.CurrentlyPlaying):
                    ChangePlaying(_playerModel.CurrentlyPlaying);
                    break;
            }
        }
        private void ChangePlaying(IPlayableItem? currentlyPlaying)
        {
            Playing = currentlyPlaying;
            //update played
            //update Queued
        }

        public void Dispose()
        {
            _playerModel.PropertyChanged -= PlayerModel_PropertyChanged;
        }
    }
}

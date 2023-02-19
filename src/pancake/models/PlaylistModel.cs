using pancake.lib;
using pancake.spotify;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pancake.models
{
    internal class PlaylistModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IPlayerModel _playerModel;
        private readonly IAPI _clientFactory;
        public IPlayableItem? _playing;
        private ISpotifyClient? _client = null;

        private RepeatingRun _queueRefresher;

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

        public PlaylistModel(IPlayerModel playerModel, IConfig config, IAPI clientFactory, ILogging logging)
        {
            _clientFactory = clientFactory;
            _clientFactory.PropertyChanged += _clientFactory_PropertyChanged;

            _playerModel = playerModel;
            _playerModel.PropertyChanged += _playerModel_PropertyChanged;


            Played = new ObservableCollection<IPlayableItem>();
            Queued = new ObservableCollection<IPlayableItem>();
            Playing = null;

            _queueRefresher = new RepeatingRun(_RepeatedlyRefreshQueue, config.RefreshDelayMS);
        }

        private async void _clientFactory_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(API.HasToken))
            {
                _queueRefresher.Stop();

                if (_clientFactory.HasToken)
                {
                    _client = _clientFactory.CreateClient();

                    await _queueRefresher.Invoke();
                    _queueRefresher.Start();
                }
                else
                {
                    _client = null;
                }
            }
        }

        private void _playerModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(PlayerModel.CurrentlyPlaying):
                    ChangePlaying(_playerModel.CurrentlyPlaying);
                    break;
            }
        }

        private async Task _RepeatedlyRefreshQueue(CancellationToken cancelToken)
        {
            //await _TryApiCall(async () =>
            //{
            //    try
            //    {
            //        await _RefreshState(cancelToken);
            //    }
            //    catch (APIException e) when
            //        (e.Message == "Player command failed: No active device found")
            //    {
            //        ClientAvailable = false;
            //    }
            //});

            await Task.FromResult(0);
        }

        private void ChangePlaying(IPlayableItem? currentlyPlaying)
        {
            Playing = currentlyPlaying;
            //update played
            //update Queued
        }

        public void Dispose()
        {
            _clientFactory.PropertyChanged -= _clientFactory_PropertyChanged;
            _playerModel.PropertyChanged -= _playerModel_PropertyChanged;
        }
    }
}

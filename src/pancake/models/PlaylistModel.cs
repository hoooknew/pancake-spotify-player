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
    public interface IPlaylistModel
    {
        ObservableCollection<PlayableItemModel> Played { get; }
        PlayableItemModel? Playing { get; }
        ObservableCollection<PlayableItemModel> Queued { get; }
    }

    public class PlaylistModel : INotifyPropertyChanged, IDisposable, IPlaylistModel
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IPlayerModel _playerModel;
        private readonly IAPI _api;
        public PlayableItemModel? _playing;
        private ISpotifyClient? _client = null;

        private RepeatingRun _queueRefresher;

        public ObservableCollection<PlayableItemModel> Played { get; private set; }
        public PlayableItemModel? Playing
        {
            get => _playing;
            private set
            {
                _playing = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Playing)));
            }
        }
        public ObservableCollection<PlayableItemModel> Queued { get; private set; }

        public PlaylistModel(IPlayerModel playerModel, IConfig config, IAPI api, ILogging logging)
        {
            _api = api;
            _api.PropertyChanged += _api_PropertyChanged;

            _playerModel = playerModel;
            _playerModel.PropertyChanged += _playerModel_PropertyChanged;


            Played = new ObservableCollection<PlayableItemModel>();
            Queued = new ObservableCollection<PlayableItemModel>();
            Playing = null;

            _queueRefresher = new RepeatingRun(_RepeatedlyRefreshQueue, config.RefreshDelayMS);
        }

        private async void _api_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(API.HasToken))
            {
                _queueRefresher.Stop();

                if (_api.HasToken)
                {
                    _client = _api.CreateClient();

                    await _queueRefresher.Invoke();
                    _queueRefresher.Start();
                }
                else
                {
                    _client = null;
                }
            }
        }

        private async void _playerModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlayerModel.CurrentlyPlaying):
                    await ChangePlaying(_playerModel.CurrentlyPlaying);
                    break;
            }
        }

        private async Task RefreshQueue(CancellationToken cancelToken = default(CancellationToken))
        {
            await _queueRefresher.Invoke();
        }

        private async Task _RepeatedlyRefreshQueue(CancellationToken cancelToken = default(CancellationToken))
        {
            //await _api.TryApiCall(async client =>
            //{
            //    var response = await client.Player.GetQueue(cancelToken);
            //    var queue = response.Queue.Select(r => new PlayableItemModel(r)).ToList();

            //    Differ<PlayableItemModel>.Instance.ApplyDiffsToOld(this.Queued, queue, r => r.Id!);
            //});

            await Task.Delay(0);
        }

        private async Task RefreshPlayed(CancellationToken cancelToken = default(CancellationToken))
        {
            //await _api.TryApiCall(async client =>
            //{
            //    var played = new List<PlayableItemModel>();

            //    var response = await client.Player.GetRecentlyPlayed(cancelToken);
            //    if (response != null && response.Items != null)
            //        played = response.Items.Select(r => r.Track).OfType<IPlayableItem>().Select(r => new PlayableItemModel(r)).ToList();

            //    Differ<PlayableItemModel>.Instance.ApplyDiffsToOld(this.Played, played, r => r.Id);
            //});

            await Task.Delay(0);
        }

        private async Task ChangePlaying(IPlayableItem? currentlyPlaying)
        {
            Playing = currentlyPlaying != null ? new PlayableItemModel(currentlyPlaying) : null;
            await RefreshPlayed();
            await RefreshQueue();
        }

        public void Dispose()
        {
            _api.PropertyChanged -= _api_PropertyChanged;
            _playerModel.PropertyChanged -= _playerModel_PropertyChanged;
        }
    }
}

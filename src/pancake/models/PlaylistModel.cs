using Microsoft.Extensions.Logging;
using pancake.lib;
using pancake.spotify;
using pancake.ui;
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
        PlayableItemModel? Playing { get; }
        int QueuedLength { get; set; }
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
        private readonly IDispatchProvider _dispatch;
        private readonly ILogger _log;

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

        public int QueuedLength { get; set; } = 4;

        public PlaylistModel(IPlayerModel playerModel, IConfig config, IDispatchProvider dispatch, IAPI api, ILogging logging)
        {
            _log = logging.Create<PlaylistModel>();

            _api = api;
            _api.PropertyChanged += _api_PropertyChanged;

            _playerModel = playerModel;
            _playerModel.PropertyChanged += _playerModel_PropertyChanged;

            _dispatch = dispatch;

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
            await _api.TryApiCall(async client =>
            {
                var response = await client.Player.GetQueue(cancelToken);
                var queue = response.Queue.Select(r => new PlayableItemModel(r)).Take(QueuedLength).ToList();

                _log.LogInformation("queue data updated. num items:{0}", response.Queue.Count());

                _dispatch.Invoke(() =>
                {
                    Differ<PlayableItemModel>.Instance.ApplyDiffsToOld(this.Queued, queue, r => r.Id!);
                });
            });            
        }

        private async Task ChangePlaying(IPlayableItem? currentlyPlaying)
        {
            Playing = currentlyPlaying != null ? new PlayableItemModel(currentlyPlaying) : null;
            await RefreshQueue();
        }

        public void Dispose()
        {
            _api.PropertyChanged -= _api_PropertyChanged;
            _playerModel.PropertyChanged -= _playerModel_PropertyChanged;
        }
    }
}

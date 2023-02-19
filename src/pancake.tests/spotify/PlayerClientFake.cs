using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.tests.spotify
{
    internal class PlayerClientFake : IPlayerClient
    {
        public Func<CurrentlyPlayingContext>? Callback_GetCurrentPlayback { get; set; }
        public Func<PlayerResumePlaybackRequest?, bool> Callback_ResumePlayback { get; set; } = request => true;

        public Task<bool> AddToQueue(PlayerAddToQueueRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeviceResponse> GetAvailableDevices(CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<CurrentlyPlaying> GetCurrentlyPlaying(PlayerCurrentlyPlayingRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<CurrentlyPlayingContext> GetCurrentPlayback(CancellationToken cancel = default)
        {
            return Task.FromResult(Callback_GetCurrentPlayback!());
        }

        public Task<CurrentlyPlayingContext> GetCurrentPlayback(PlayerCurrentPlaybackRequest request, CancellationToken cancel = default)
        {
            return Task.FromResult(Callback_GetCurrentPlayback!());
        }

        public Task<QueueResponse> GetQueue(CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<CursorPaging<PlayHistoryItem>> GetRecentlyPlayed(CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<CursorPaging<PlayHistoryItem>> GetRecentlyPlayed(PlayerRecentlyPlayedRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PausePlayback(CancellationToken cancel = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> PausePlayback(PlayerPausePlaybackRequest request, CancellationToken cancel = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ResumePlayback(CancellationToken cancel = default)
        {
            return Task.FromResult(Callback_ResumePlayback(null));
        }

        public Task<bool> ResumePlayback(PlayerResumePlaybackRequest request, CancellationToken cancel = default)
        {
            return Task.FromResult(Callback_ResumePlayback(request));
        }

        public Task<bool> SeekTo(PlayerSeekToRequest request, CancellationToken cancel = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> SetRepeat(PlayerSetRepeatRequest request, CancellationToken cancel = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> SetShuffle(PlayerShuffleRequest request, CancellationToken cancel = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> SetVolume(PlayerVolumeRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SkipNext(CancellationToken cancel = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> SkipNext(PlayerSkipNextRequest request, CancellationToken cancel = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> SkipPrevious(CancellationToken cancel = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> SkipPrevious(PlayerSkipPreviousRequest request, CancellationToken cancel = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> TransferPlayback(PlayerTransferPlaybackRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }
    }
}

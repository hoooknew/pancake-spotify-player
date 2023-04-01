using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.tests.spotify
{
    internal class LibraryClientFake : ILibraryClient
    {
        public Task<List<bool>> CheckAlbums(LibraryCheckAlbumsRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<bool>> CheckEpisodes(LibraryCheckEpisodesRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<bool>> CheckShows(LibraryCheckShowsRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<bool>> CheckTracks(LibraryCheckTracksRequest request, CancellationToken cancel = default)
        {
            return Task.FromResult(request.Ids.Select(r => true).ToList());
        }

        public Task<Paging<SavedAlbum>> GetAlbums(CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<Paging<SavedAlbum>> GetAlbums(LibraryAlbumsRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<Paging<SavedEpisodes>> GetEpisodes(CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<Paging<SavedEpisodes>> GetEpisodes(LibraryEpisodesRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<Paging<SavedShow>> GetShows(CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<Paging<SavedShow>> GetShows(LibraryShowsRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<Paging<SavedTrack>> GetTracks(CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<Paging<SavedTrack>> GetTracks(LibraryTracksRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveAlbums(LibraryRemoveAlbumsRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveEpisodes(LibraryRemoveEpisodesRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveShows(LibraryRemoveShowsRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveTracks(LibraryRemoveTracksRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveAlbums(LibrarySaveAlbumsRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveEpisodes(LibrarySaveEpisodesRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveShows(LibrarySaveShowsRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveTracks(LibrarySaveTracksRequest request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }
    }
}

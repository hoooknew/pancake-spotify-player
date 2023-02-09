using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.tests.spotify
{
    internal class SpotifyClientFake : ISpotifyClient
    {
        public SpotifyClientFake() { }
        public IPaginator DefaultPaginator => throw new NotImplementedException();

        public IUserProfileClient UserProfile => throw new NotImplementedException();

        public IBrowseClient Browse => throw new NotImplementedException();

        public IShowsClient Shows => throw new NotImplementedException();

        public IPlaylistsClient Playlists => throw new NotImplementedException();

        public ISearchClient Search => throw new NotImplementedException();

        public IFollowClient Follow => throw new NotImplementedException();

        public ITracksClient Tracks => throw new NotImplementedException();

        public PlayerClientFake PlayerFake { get; } = new PlayerClientFake();
        public IPlayerClient Player => PlayerFake;

        public IAlbumsClient Albums => throw new NotImplementedException();

        public IArtistsClient Artists => throw new NotImplementedException();

        public IPersonalizationClient Personalization => throw new NotImplementedException();

        public IEpisodesClient Episodes => throw new NotImplementedException();

        public LibraryClientFake LibraryFake { get; } = new LibraryClientFake();
        public ILibraryClient Library => LibraryFake;

        public IResponse? LastResponse => throw new NotImplementedException();

        public Task<Paging<T>> NextPage<T>(Paging<T> paging)
        {
            throw new NotImplementedException();
        }

        public Task<CursorPaging<T>> NextPage<T>(CursorPaging<T> cursorPaging)
        {
            throw new NotImplementedException();
        }

        public Task<TNext> NextPage<T, TNext>(IPaginatable<T, TNext> paginatable)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<T> Paginate<T>(IPaginatable<T> firstPage, IPaginator? paginator = null, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<T> Paginate<T, TNext>(IPaginatable<T, TNext> firstPage, Func<TNext, IPaginatable<T, TNext>> mapper, IPaginator? paginator = null, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<IList<T>> PaginateAll<T>(IPaginatable<T> firstPage, IPaginator? paginator = null)
        {
            throw new NotImplementedException();
        }

        public Task<IList<T>> PaginateAll<T, TNext>(IPaginatable<T, TNext> firstPage, Func<TNext, IPaginatable<T, TNext>> mapper, IPaginator? paginator = null)
        {
            throw new NotImplementedException();
        }

        public Task<Paging<T>> PreviousPage<T>(Paging<T> paging)
        {
            throw new NotImplementedException();
        }

        public Task<TNext> PreviousPage<T, TNext>(Paging<T, TNext> paging)
        {
            throw new NotImplementedException();
        }
    }
}

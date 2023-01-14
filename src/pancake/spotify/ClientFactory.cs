using pancake.lib;
using pancake.ui.controls;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.spotify
{
    public interface IClientFactory
    {
        ISpotifyClient CreateClient(IRefreshableToken token);
    }

    public class ClientFactory : IClientFactory
    {
        public ISpotifyClient CreateClient(IRefreshableToken token)
        {
            var authenticator = Authentication.CreateAuthenticator(token);

            var config = SpotifyClientConfig.CreateDefault()
            .WithAuthenticator(authenticator);

            return new SpotifyClient(config);
        }
    }
}

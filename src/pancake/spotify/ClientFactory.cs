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
        ISpotifyClient CreateClient(object token);
    }

    public class ClientFactory : IClientFactory
    {
        private readonly IAuthentication _auth;

        public ClientFactory(IAuthentication auth) 
        {
            _auth = auth;
        }
        public ISpotifyClient CreateClient(object token)
        {
            var authenticator = _auth.CreateAuthenticator(token);

            var config = SpotifyClientConfig.CreateDefault()
            .WithAuthenticator(authenticator);

            return new SpotifyClient(config);
        }
    }
}

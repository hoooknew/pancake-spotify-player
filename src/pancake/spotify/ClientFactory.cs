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
        event EventHandler? TokenChanged;

        void SetToken(object? token);

        bool HasToken { get; }

        ISpotifyClient CreateClient();
    }

    public class ClientFactory : IClientFactory
    {
        public event EventHandler? TokenChanged;

        private readonly IAuthentication _auth;
        private object? _token;

        public ClientFactory(IAuthentication auth) 
        {
            _auth = auth;
        }

        public void SetToken(object? token)
        {
            _token = token;
            TokenChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool HasToken => _token != null;

        public ISpotifyClient CreateClient()
        {
            if (_token == null)
                throw new ArgumentException("no token set.");
            else
            {
                var authenticator = _auth.CreateAuthenticator(_token);

                var config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(authenticator);

                return new SpotifyClient(config);
            }
        }
    }
}

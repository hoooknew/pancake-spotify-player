using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace test_auth
{
    public class Program
    {
        private static EmbedIOAuthServer? _server;

        public static async Task Main()
        {
            var builder = new ConfigurationBuilder()
               .AddJsonFile($"appsettings.json");
            var configuration = builder.Build();

            string? clientId = configuration["clientId"];
            var (verifier, challenge) = PKCEUtil.GenerateCodes();

            _server = new EmbedIOAuthServer(new Uri("http://localhost:3000/auth/callback"), 3000);
            await _server.Start();

            //_server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.AuthorizationCodeReceived += async (object sender, AuthorizationCodeResponse response) =>
            {
                await _server.Stop();
                var token = await new OAuthClient().RequestToken(
                    new PKCETokenRequest(clientId!, response.Code, _server.BaseUri, verifier)
                );

                var spotify = new SpotifyClient(token.AccessToken);
            };
            

            var request = new LoginRequest(_server.BaseUri, clientId!, LoginRequest.ResponseType.Code)
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
                Scope = new List<string> 
                { 
                    Scopes.AppRemoteControl, 
                    Scopes.UserReadCurrentlyPlaying, 
                    Scopes.UserReadPlaybackState, 
                    Scopes.UserReadPlaybackPosition, 
                    Scopes.UserModifyPlaybackState 
                }
            };

            Uri uri = request.ToUri();
            try
            {
                BrowserUtil.Open(uri);
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to open URL, manually open: {0}", uri);
            }

            Console.ReadKey();
        }

        //private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        //{            

        //    var oauth = new OAuthClient();

        //    var tokenRequest = new TokenSwapTokenRequest(new Uri("http://localhost:5001/swap"), response.Code);
        //    var tokenResponse = await oauth.RequestToken(tokenRequest);

        //    Console.WriteLine($"We got an access token from server: {tokenResponse.AccessToken}");

        //    var refreshRequest = new TokenSwapRefreshRequest(
        //      new Uri("http://localhost:5001/refresh"),
        //      tokenResponse.RefreshToken
        //    );
        //    var refreshResponse = await oauth.RequestToken(refreshRequest);

        //    Console.WriteLine($"We got a new refreshed access token from server: {refreshResponse.AccessToken}");
        //}
    }
}
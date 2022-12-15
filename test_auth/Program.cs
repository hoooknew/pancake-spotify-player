using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using Swan.Logging;
using static SpotifyAPI.Web.Scopes;

namespace test_auth
{
    //https://github.com/JohnnyCrazy/SpotifyAPI-NET/blob/54f8f8960fbd859781fd971efaca94462ca52468/SpotifyAPI.Web.Examples/Example.CLI.PersistentConfig/Program.cs
    /// <summary>
    ///   This is a basic example how to get user access using the Auth package and a CLI Program
    ///   Your spotify app needs to have http://localhost:3000 as redirect uri whitelisted
    /// </summary>
    public class test_auth
    {
        private const string CredentialsPath = "credentials.json";        
        private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:3000/auth/callback"), 3000);

        private static void Exiting() => Console.CursorVisible = true;
        public static async Task<int> Main()
        {
            // This is a bug in the SWAN Logging library, need this hack to bring back the cursor
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Exiting();

            //if (string.IsNullOrEmpty(Config.ClientId))
            //{
            //    throw new NullReferenceException(
            //      "Please set SPOTIFY_CLIENT_ID via environment variables before starting the program"
            //    );
            //}

            //if (File.Exists(CredentialsPath))
            //{
            //    await Start();
            //}
            //else
            //{
            //    await StartAuthentication();
            //}

            //Console.ReadKey();

            var creds = await Authenticator.Login();

            return 0;
        }

        private static async Task Start()
        {
            var json = await File.ReadAllTextAsync(CredentialsPath);
            var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

            var authenticator = new PKCEAuthenticator(Config.ClientId!, token!);
            authenticator.TokenRefreshed += (sender, token) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(token));

            var config = SpotifyClientConfig.CreateDefault()
              .WithAuthenticator(authenticator);

            var spotify = new SpotifyClient(config);

            var me = await spotify.UserProfile.Current();
            Console.WriteLine($"Welcome {me.DisplayName} ({me.Id}), you're authenticated!");

            var playlists = await spotify.PaginateAll(await spotify.Playlists.CurrentUsers().ConfigureAwait(false));
            Console.WriteLine($"Total Playlists in your Account: {playlists.Count}");

            _server.Dispose();
            Environment.Exit(0);
        }

        private static async Task StartAuthentication()
        {
            var (verifier, challenge) = PKCEUtil.GenerateCodes();

            await _server.Start();
            _server.AuthorizationCodeReceived += async (sender, response) =>
            {
                await _server.Stop();
                PKCETokenResponse token = await new OAuthClient().RequestToken(
                  new PKCETokenRequest(Config.ClientId!, response.Code, _server.BaseUri, verifier)
                );

                await File.WriteAllTextAsync(CredentialsPath, JsonConvert.SerializeObject(token));
                await Start();
            };

            var request = new LoginRequest(_server.BaseUri, Config.ClientId!, LoginRequest.ResponseType.Code)
            {
                CodeChallenge = challenge,
                CodeChallengeMethod = "S256",
                Scope = new List<string> { 
                    AppRemoteControl,
                    UserReadCurrentlyPlaying,
                    UserReadPlaybackState,
                    UserReadPlaybackPosition,
                    UserModifyPlaybackState,
                    UserReadEmail, 
                    UserReadPrivate, 
                    PlaylistReadPrivate, 
                    PlaylistReadCollaborative }
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
        }
    }
}
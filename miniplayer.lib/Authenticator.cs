using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniplayer.lib
{
    public static class Authentication
    {
        private static readonly string _credentialsPath = Path.Combine(Constants.LOCAL_APP_DATA, @"credentials.json");
        
        public static async Task<IRefreshableToken?> Login()
        {
            if (string.IsNullOrEmpty(Config.ClientId))
                throw new NullReferenceException(
                  "Please set SPOTIFY_CLIENT_ID via environment variables before starting the program"
                );

            using (EmbedIOAuthServer server = new EmbedIOAuthServer(new Uri("http://localhost:3000/auth/callback"), 3000))
            {

                var (verifier, challenge) = PKCEUtil.GenerateCodes();

                using (ManualResetEvent resetEvent = new ManualResetEvent(false))
                {
                    var syncObj = new object();
                    object? result = null;

                    await server.Start();
                    server.AuthorizationCodeReceived += async (sender, response) =>
                    {
                        await server.Stop();

                        PKCETokenResponse token = await new OAuthClient().RequestToken(
                          new PKCETokenRequest(Config.ClientId!, response.Code, server.BaseUri, verifier)
                        );

                        lock (syncObj)
                        {
                            result = token;
                        }
                        resetEvent.Set();
                    };

                    server.ErrorReceived += async (sender, error, state) =>
                    {
                        await server.Stop();

                        lock (syncObj)
                        {
                            result = error;
                        }
                        resetEvent.Set();
                    };

                    var request = new LoginRequest(server.BaseUri, Config.ClientId!, LoginRequest.ResponseType.Code)
                    {
                        CodeChallenge = challenge,
                        CodeChallengeMethod = "S256",
                        //https://developer.spotify.com/documentation/general/guides/authorization/scopes/
                        Scope = new List<string> {
                            Scopes.UserReadCurrentlyPlaying,
                            Scopes.UserReadPlaybackState,
                            Scopes.UserModifyPlaybackState,
                            Scopes.UserLibraryRead,
                            Scopes.UserLibraryModify
                        }
                    };

                    var uri = request.ToUri();
                    try
                    {
                        uri.CallWithShell();
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine("Unable to open URL, manually open: {0}", uri);
                    }

                    if (resetEvent.WaitOne(60_000) && result != null)
                    {
                        if (result is IRefreshableToken token)
                            return token;
                        else if (result is string error)
                        {
                            Console.Error.WriteLine($"Error authenticating: {error}");
                            return null;
                        }
                        else
                        {
                            await server.Stop();
                            return null;
                        }
                    }
                    else
                    {
                        await server.Stop();
                        Console.Error.WriteLine($"Timeout waiting for an authorization response.");
                        return null;
                    }
                }
            }
        }

        public static IAuthenticator CreateAuthenticator(IRefreshableToken? token)
        {
            var authenticator = new PKCEAuthenticator(Config.ClientId!, (token as PKCETokenResponse)!);
            authenticator.TokenRefreshed += (sender, token) => SaveToken(token);

            return authenticator;
        }


        public static bool TokenAvailable() => File.Exists(_credentialsPath);
        public static void SaveToken(IRefreshableToken? token)
        {
            CreateLocalAppFolder();

            if (token == null && File.Exists(_credentialsPath))
                File.Delete(_credentialsPath);
            else
                File.WriteAllText(_credentialsPath, JsonConvert.SerializeObject(token));
        }        

        public static IRefreshableToken? LoadToken()
        {
            if (TokenAvailable())
            {
                var json = File.ReadAllText(_credentialsPath);
                return JsonConvert.DeserializeObject<PKCETokenResponse>(json);
            }
            else
                return null;
        }

        private static void CreateLocalAppFolder()
        {
            if (!Directory.Exists(Constants.LOCAL_APP_DATA))
                Directory.CreateDirectory(Constants.LOCAL_APP_DATA);
        }
    }
}

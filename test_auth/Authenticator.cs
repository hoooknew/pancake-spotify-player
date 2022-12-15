using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test_auth
{
    internal class Authenticator
    {
        public static async Task<PKCETokenResponse?> Login()
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
                        Scope = new List<string> {
                    Scopes.AppRemoteControl,
                    Scopes.UserReadCurrentlyPlaying,
                    Scopes.UserReadPlaybackState,
                    Scopes.UserReadPlaybackPosition,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadEmail,
                    Scopes.UserReadPrivate,
                    Scopes.PlaylistReadPrivate,
                    Scopes.PlaylistReadCollaborative }
                    };

                    Uri uri = request.ToUri();
                    try
                    {
                        BrowserUtil.Open(uri);
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine("Unable to open URL, manually open: {0}", uri);
                    }

                    if (resetEvent.WaitOne(20_000) && result != null)
                    {
                        if (result is PKCETokenResponse token)
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
                        return null;
                    }
                }
            }
        }
    }
}

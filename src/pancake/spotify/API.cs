﻿using Microsoft.Extensions.Logging;
using pancake.lib;
using pancake.models;
using pancake.ui.controls;
using SpotifyAPI.Web;
using Swan.Parsers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace pancake.spotify
{
    public interface IAPI
    {
        event EventHandler<ApiErrorEventArgs>? Error;
        event PropertyChangedEventHandler? PropertyChanged;

        void SetToken(object? token);

        bool HasToken { get; }
        bool ClientAvailable { get; set; }

        ISpotifyClient CreateClient();

        Task<bool> TryApiCall(Func<ISpotifyClient, Task> a);

        void HandleAPIError(Exception e);
    }

    public class API : IAPI, INotifyPropertyChanged
    {
        public event EventHandler<ApiErrorEventArgs>? Error;
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IAuthentication _auth;
        private object? _token;
        private bool _clientAvailable = false;
        private readonly ILogger _log;

        public API(IAuthentication auth, ILogging logging) 
        {
            _auth = auth;
            _log = logging.Create<API>();
        }

        public void SetToken(object? token)
        {
            _token = token;
            _client = null;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasToken)));

            _log.LogInformation("token set: {0}", token != null ? "<NOT NULL>" : "<NULL>");
        }

        public bool HasToken => _token != null;
        public bool ClientAvailable 
        { 
            get => _clientAvailable; 
            set
            {
                _clientAvailable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClientAvailable)));
            }
        }

        public ISpotifyClient CreateClient()
        {
            if (_token == null)
                throw new ArgumentException("no token set.");
            else
            {
                var authenticator = _auth.CreateAuthenticator(_token);

                var config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(authenticator);

                _log.LogInformation("create client, token: {0}", _token != null ? "<NOT NULL>" : "<NULL>");

                return new SpotifyClient(config);
            }
        }

        private ISpotifyClient? _client = null;
        private ISpotifyClient GetClient()
        {
            if (_client == null)
                _client = CreateClient();

            return _client;
        }

        public async Task<bool> TryApiCall(Func<ISpotifyClient, Task> func)
        {
            try
            {
                int retriesLeft = 3;
                while (retriesLeft > 0)
                {
                    try
                    {
                        await func(GetClient());

                        ClientAvailable = true;

                        return true;
                    }
                    catch (APITooManyRequestsException e)
                    {
                        await Task.Delay(e.RetryAfter);
                        _log.LogError("Too Many Requests Error");
                    }
                    catch (APIException e) when (e.Message == "Player command failed: Restriction violated")
                    {
                        //retry
                        _log.LogError("API Error: {0}", e.Message);
                    }
                    catch (APIException e) when
                        (e.Message == "Service unavailable")
                    {
                        //fail silently
                        _log.LogError("API Error: {0}", e.Message);
                        break;
                    }
                    catch (APIException e) when
                        (e.Message == "Player command failed: No active device found")
                    {
                        ClientAvailable = false;
                        _log.LogError("API Error: {0}", e.Message);
                        break;
                    }
                    catch (HttpRequestException e) when
                        (e.Message == "No such host is known. (api.spotify.com:443)")
                    {
                        _log.LogError("Http Request Error: {0}", e.Message);
                        //should we have a no connection state?
                        break;
                    }
                    /* the client blew up on some ssl exception at some point, 
                     * but I didn't record the exception type. This is where it
                     * should be handled. */
                    //catch(HttpException) 
                    //{

                    //}

                    retriesLeft--;
                }

                return false;
            }
            catch (APIException e) when (e is not APITooManyRequestsException)
            {
                //_dispatcher.Invoke(() => _HandleAPIError(e));
                _log.LogError("API Error: {0}", e.Message);
                HandleAPIError(e);
                return false;
            }
        }

        public void HandleAPIError(Exception e)
        {
            Error?.Invoke(this, new ApiErrorEventArgs(e));            
        }
    }
}

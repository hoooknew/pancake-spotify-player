﻿using pancake.lib;
using pancake.models;
using pancake.ui.controls;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace pancake.spotify
{
    public interface IClientFactory
    {
        event EventHandler<ApiErrorEventArgs>? ApiError;
        event PropertyChangedEventHandler? PropertyChanged;

        void SetToken(object? token);

        bool HasToken { get; }
        bool ClientAvailable { get; set; }

        ISpotifyClient CreateClient();

        Task<bool> TryApiCall(Func<Task> a);

        void HandleAPIError(Exception e);
    }

    public class ClientFactory : IClientFactory, INotifyPropertyChanged
    {
        public event EventHandler<ApiErrorEventArgs>? ApiError;
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IAuthentication _auth;
        private object? _token;
        private bool _clientAvailable = false;

        public ClientFactory(IAuthentication auth) 
        {
            _auth = auth;
        }

        public void SetToken(object? token)
        {
            _token = token;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasToken)));
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

                return new SpotifyClient(config);
            }
        }

        public async Task<bool> TryApiCall(Func<Task> a)
        {
            try
            {
                int retriesLeft = 3;
                while (retriesLeft > 0)
                {
                    try
                    {
                        await a();

                        ClientAvailable = true;

                        return true;
                    }
                    catch (APITooManyRequestsException e)
                    {
                        await Task.Delay(e.RetryAfter);
                    }
                    catch (APIException e) when (e.Message == "Player command failed: Restriction violated")
                    {
                        //retry
                    }
                    catch (APIException e) when
                        (e.Message == "Service unavailable")
                    {
                        //fail silently
                        break;
                    }
                    catch (APIException e) when
                        (e.Message == "Player command failed: No active device found")
                    {
                        ClientAvailable = false;
                        break;
                    }
                    catch (HttpRequestException e) when
                        (e.Message == "No such host is known. (api.spotify.com:443)")
                    {
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
                HandleAPIError(e);
                return false;
            }
        }

        public void HandleAPIError(Exception e)
        {
            ApiError?.Invoke(this, new ApiErrorEventArgs(e));            
        }
    }
}

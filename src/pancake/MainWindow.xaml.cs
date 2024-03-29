﻿using Microsoft.Extensions.Logging;
using pancake.lib;
using pancake.models;
using pancake.spotify;
using pancake.ui;
using pancake.ui.controls;
using SpotifyAPI.Web;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace pancake
{
    public partial class MainWindow : BaseWindow, IDisposable
    {
        readonly ILogger<MainWindow> _logger;
        private readonly IAuthentication _auth;
        private readonly IAPI _api;
        readonly IPlayerModel _model;
        bool _commandExecuting = false;
        readonly DockableWindows _dockable;
        readonly PlaylistWindow _playlist;

        public MainWindow(PlaylistWindow playlist, ILogging logging, IAuthentication auth, IPlayerModel model, IAPI api, IDispatchProvider dispatchProvider)
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;

            _logger = logging.Create<MainWindow>();
            _auth = auth;
            _api = api;
            _api.Error += _api_Error;
            _model = model;

            _dockable = new DockableWindows(this, logging, dispatchProvider);

            _playlist = playlist;
            _setupPlaylist();
        }

        private void _setupPlaylist()
        {
            _playlist.Closing += (s, e) =>
            {
                e.Cancel = true;
                _playlist.Hide();
                Settings.Instance.PlaylistVisible = false;
            };
            _dockable.DockWindowTo(_playlist, DockableWindows.DockedTo.Top_Primary | DockableWindows.DockedTo.Left_Secondary | DockableWindows.DockedTo.Right_Secondary);


            SetPlaylistVisibility();
        }

        protected override void OnActivated(EventArgs e)
        {
            if (_playlist != null)
            {
                //this makes the playlist visible if it's behind another window and the main window is activated.
                _playlist.Topmost = true;
                _playlist.Topmost = false;
            }
            base.OnActivated(e);
        }

        private void _api_Error(object? sender, ApiErrorEventArgs e)
        {
            _model.SignOut();
            _auth.SaveToken(null);

            if (!(e.Exception is APIUnauthorizedException))
            {
                _logger.LogError(e.Exception, "api error: {0}", e.Exception.Message);
                MessageBox.Show(e.Exception.Message);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            object? token;
            if (_auth.TokenAvailable())
            {
                token = _auth.LoadToken();

                if (token != null)
                    _api.SetToken(token);
            }
            else
                token = null;

            this.DataContext = _model;
        }

        private async void PlayerCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (!_commandExecuting)
            {
                try
                {
                    _commandExecuting = true;

                    if (e.Command == PlayerCommands.SignIn)
                        await SignIn();
                    else if (e.Command == PlayerCommands.Shuffle)
                        await _model.ToggleShuffle();
                    else if (e.Command == PlayerCommands.SkipPrev)
                        await _model.SkipPrevious();
                    else if (e.Command == PlayerCommands.PlayPause)
                        await _model.PlayPause();
                    else if (e.Command == PlayerCommands.SkipNext)
                        await _model.SkipNext();
                    else if (e.Command == PlayerCommands.Repeat)
                        await _model.ToggleRepeat();
                    else if (e.Command == PlayerCommands.Favorite)
                        await _model.ToggleFavorite();
                }
                finally
                {
                    _commandExecuting = false;
                }
            }
            else
                Debug.WriteLine("ignored command");
        }

        private async Task SignIn()
        {
            var token = await _auth.Login();
            _auth.SaveToken(token);
            if (token != null)
                _api.SetToken(token);
        }

        private void SettingsCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (e.Command == SettingsCommands.ChangeTheme)
            {
                if (e.Parameter is string newTheme)
                {
                    if (Settings.Instance.Theme != newTheme)
                    {
                        (App.Current as App)!.SetTheme(newTheme);
                        Settings.Instance.Theme = newTheme;
                        Settings.Instance.Save();
                    }
                }
            }
            else if (e.Command == SettingsCommands.ToggleAlwaysOnTop)
            {
                Settings.Instance.AlwaysOnTop = !Settings.Instance.AlwaysOnTop;
                Settings.Instance.Save();
            }
            else if (e.Command == SettingsCommands.HideShowControls)
            {
                Settings.Instance.ControlsVisible = !Settings.Instance.ControlsVisible;
                Settings.Instance.Save();
            }
            else if (e.Command == SettingsCommands.HideShowProgress)
            {
                Settings.Instance.ProgressVisible = !Settings.Instance.ProgressVisible;
                Settings.Instance.Save();
            }
            else if (e.Command == SettingsCommands.HideShowTaskbar)
            {
                Settings.Instance.TaskbarVisible = !Settings.Instance.TaskbarVisible;
                Settings.Instance.Save();
            }
            else if (e.Command == SettingsCommands.SignOut)
            {
                _auth.SaveToken(null);
                _model.SignOut();
            }
            else if (e.Command == SettingsCommands.ChangeZoom)
            {
                if (e.Parameter is double uiScale)
                {
                    if (Settings.Instance.UiScale != uiScale)
                    {
                        Settings.Instance.UiScale = uiScale;
                        Settings.Instance.Save();
                        this.Dispatcher.BeginInvoke(() => _dockable.PositionDockedWindows(), DispatcherPriority.Background);
                    }
                }
            }
            else if (e.Command == SettingsCommands.HideShowPlaylist)
            {
                Settings.Instance.PlaylistVisible = !Settings.Instance.PlaylistVisible;
                SetPlaylistVisibility();
            }
        }

        private void SetPlaylistVisibility()
        {
            if (Settings.Instance.PlaylistVisible)
                this.ShowPlaylist();
            else
                this.HidePlaylist();
        }

        private void ShowPlaylist()
        {
            _playlist.Show();
        }

        private void HidePlaylist()
        {
            _playlist.Hide();
        }

        private void OpenInSpotify_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Spotify.Open(e.Parameter);
        }

        public void Dispose()
        {
            _dockable?.Dispose();
        }
    }
}
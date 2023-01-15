using Microsoft.Extensions.Logging;
using pancake.lib;
using pancake.models;
using pancake.ui;
using pancake.ui.controls;
using SpotifyAPI.Web;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace pancake
{
    public partial class MainWindow : BaseWindow
    {
        readonly ILogger<MainWindow> _logger;
        private readonly IAuthentication _auth;
        readonly PlayerModel _model;
        bool _commandExecuting = false;

        public MainWindow(ILogging logging, IAuthentication auth, PlayerModel model)
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            _logger = logging.Create<MainWindow>();
            _auth = auth;
            _model = model;
            _model.ApiError += _model_ApiError;
        }

        private void _model_ApiError(object? sender, ApiErrorEventArgs e)
        {
            //is this on a worker thread? does it need to be invoked?
            this.Dispatcher.Invoke(() =>
            {
                this._model.SignOut();
                _auth.SaveToken(null);

                if (!(e.Exception is APIUnauthorizedException))
                {
                    _logger.LogError(e.Exception, "api error: {0}", e.Exception.Message);
                    MessageBox.Show(e.Exception.Message);
                }
            });
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IRefreshableToken? token;
            if (_auth.TokenAvailable())
            {
                token = _auth.LoadToken();

                if (token != null)
                    _model.SetToken(token);
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
                _model.SetToken(token);
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
                    }
                }                
            }
        }

        private void OpenInSpotify_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Spotify.Open(e.Parameter);
        }
    }
}
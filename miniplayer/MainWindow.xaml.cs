using miniplayer.lib;
using miniplayer.models;
using miniplayer.ui;
using miniplayer.ui.controls;
using SpotifyAPI.Web;
using System.Threading.Tasks;
using System.Windows;

namespace miniplayer
{
    public partial class MainWindow : BaseWindow
    {
        private readonly PlayerModel _model;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;            

            _model = new PlayerModel(this.Dispatcher);
            _model.ApiError += _model_ApiError;
        }        

        private void _model_ApiError(object? sender, ApiErrorEventArgs e)
        {
            Authentication.SaveToken(null);
            if (!(e.Exception is APIUnauthorizedException))
                MessageBox.Show(e.Exception.Message);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IRefreshableToken? token;
            if (Authentication.TokenAvailable())
            {
                token = Authentication.LoadToken();

                if (token != null)
                    _model.SetToken(token);
            }
            else
                token = null;

            this.DataContext = _model;

            this.RestoreLocation();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            this.SaveLocation();
        }
         
        private void CommandBinding_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void CommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
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

        private async Task SignIn()
        {
            var token = await Authentication.Login();
            Authentication.SaveToken(token);
            if (token != null)
                _model.SetToken(token);
        }
    }
}
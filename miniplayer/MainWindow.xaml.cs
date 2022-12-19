using miniplayer.lib;
using miniplayer.models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace miniplayer
{
    public partial class MainWindow : Window
    {
        bool ResizeInProcess = false;
        private readonly PlayerModel _model;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

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
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }


        private void Resize_Init(object sender, MouseButtonEventArgs e)
        {            
            if (sender is Rectangle senderRect)
            {
                ResizeInProcess = true;
                senderRect.CaptureMouse();                
                e.Handled = true;
            }
        }
        private void Resize_End(object sender, MouseButtonEventArgs e)
        {            
            if (sender is Rectangle senderRect)
            {
                ResizeInProcess = false; ;
                senderRect.ReleaseMouseCapture();
                e.Handled = true;                
            }
        }
        private void Resizing_Window(object sender, MouseEventArgs e)
        {            
            if (ResizeInProcess)
            {                
                if (sender is Rectangle senderRect)
                {
                    double mouseX = e.GetPosition(this).X;
                    if (senderRect == this._rightSizeGrip)
                    {
                        this.Width = Math.Max(this.MinWidth, mouseX);
                    }
                    else if (senderRect == this._leftSizeGrip)
                    {
                        this.Width = Math.Max(this.MinWidth, this.Width - mouseX);
                        this.Left = this.Left + mouseX;
                    }
                }

                e.Handled = true;
            }
        }


        private void _closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private async void _signin_Click(object sender, RoutedEventArgs e)
        {
            var token = await Authentication.Login();
            Authentication.SaveToken(token);
            if (token != null)
                _model.SetToken(token);
        }

        private async void _play_pause_btn_Click(object sender, RoutedEventArgs e)
        {
            await _model.PlayPause();
        }

        private async void _repeat_btn_Click(object sender, RoutedEventArgs e)
        {
            await _model.ToggleRepeat();
        }

        private async void _skip_next_btn_Click(object sender, RoutedEventArgs e)
        {
            await _model.SkipNext();
        }

        private async void _skip_prev_btn_Click(object sender, RoutedEventArgs e)
        {
            await _model.SkipPrevious();
        }

        private async void _shuffle_btn_Click(object sender, RoutedEventArgs e)
        {
            await _model.ToggleShuffle();
        }

        private async void _favorite_btn_Click(object sender, RoutedEventArgs e)
        {
            await _model.ToggleFavorite();
        }
    }
}

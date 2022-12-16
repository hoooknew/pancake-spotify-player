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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool ResizeInProcess = false;
        private PlayerModel? _model = null;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IRefreshableToken? token;
            if (Config.TokenAvailable())
                token = Config.LoadToken();
            else
            {
                token = await Authentication.Login();
                Config.SaveToken(token);
            }

            if (token != null)
            {
                var authenticator = Authentication.CreateAuthenticator(token);

                var config = SpotifyClientConfig.CreateDefault()
                    .WithAuthenticator(authenticator);

                var client = new SpotifyClient(config);

                _model = new PlayerModel(client, this.Dispatcher);
                this.DataContext = _model;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
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
    }
}

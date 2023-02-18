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

namespace DockToWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private DockableWindows _dw;

        private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Top = 1000;
            this.Left = 100;
            this.Width = 600;
            this.Height = 200;
            this.Topmost = true;

            _dw = new DockableWindows(this);
            //_dw.AllowTop = _dw.AllowBottom = 
            //_dw.AllowRight = _dw.AllowLeft = false;

            this.MouseRightButtonUp += MainWindow_MouseRightButtonUp;

            var dockable = new DockableWindow();
            //dockable.Topmost = true;
            dockable.Width = 590;
            dockable.Height = 190;

            _dw.AddDockable(dockable);
            dockable.Show();
        }

        private void MainWindow_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dw.Dispose();
        }
    }
}

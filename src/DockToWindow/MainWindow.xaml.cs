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
            this.Topmost = true;

            _dw = new DockableWindows(this);

            this.MouseRightButtonUp += MainWindow_MouseRightButtonUp;

            var dockable = new DockableWindow();
            dockable.Topmost = true;

            _dw.AddDockable(dockable);
            dockable.Show();
        }

        private void MainWindow_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dw.Dispose();
        }
    }
}

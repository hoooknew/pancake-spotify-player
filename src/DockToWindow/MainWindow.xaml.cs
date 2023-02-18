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

        private void _mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Top = 1000;
            this.Left = 100;
            this.Topmost = true;

            var dw = new DockableWindows(this);

            var dockable = new DockableWindow();
            dockable.Topmost = true;

            dw.AddDockable(dockable);
            dockable.Show();
        }
    }
}

using miniplayer.lib;
using miniplayer.ui.controls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace miniplayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            SetTheme();

            var w = new MainWindow();
            w.Show();
        }

        private void SetTheme()
        {
            var themeDic = this.Resources.MergedDictionaries
                            .Where(r => r.Source.ToString()
                            .StartsWith("/themes/"))
                            .FirstOrDefault();

            if (themeDic != null)
            {
                switch (Settings.Instance.Theme?.ToLower())
                {
                    case "light":
                        themeDic.Source = new Uri("/themes/light.xaml", UriKind.Relative);
                        break;
                    case "dark":
                        themeDic.Source = new Uri("/themes/dark.xaml", UriKind.Relative);
                        break;
                }
            }
        }
    }
}

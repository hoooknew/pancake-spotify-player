using miniplayer.lib;
using System;
using System.Data;
using System.Linq;
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

            MainWindow = new MainWindow();
            MainWindow.Show();
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

#if DEBUG
        public static void SaveDefaultTemplate()
        {
            var control = Application.Current.FindResource(typeof(System.Windows.Controls.ContextMenu));
            var sw = new System.IO.StringWriter();
            System.Windows.Markup.XamlWriter.Save(control, sw);
            Clipboard.SetText(sw.ToString());
        }
    }
#endif
}

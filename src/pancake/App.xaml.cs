﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using pancake.lib;
using pancake.models;
using pancake.spotify;
using pancake.ui;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace pancake
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? Host { get; private set; }
        private readonly ILogger<App> _log;

        public App()
        {
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();

            var logging = Host.Services.GetRequiredService<ILogging>();
            _log = logging.Create<App>();
            _log.LogInformation("starting...");
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Add Services
            services                
                .AddSingleton<App>(this)
                .AddSingleton(typeof(IConfig), typeof(Config))
                .AddSingleton(typeof(ILogging), typeof(Logging))
                .AddSingleton(typeof(IDispatchProvider), new DispatchProvider(this.Dispatcher))
                .AddSingleton(typeof(IAuthentication), typeof(Authentication))
                .AddSingleton(typeof(IAPI), typeof(API))
                .AddSingleton(typeof(IPlayerModel), typeof(PlayerModel))
                .AddSingleton(typeof(IPlaylistModel), typeof(PlaylistModel))
                .AddSingleton<MainWindow>()
                .AddSingleton<PlaylistWindow>();
        }

        protected async override void OnStartup(StartupEventArgs e)
        {
            SetTheme(Settings.Instance.Theme);

            await Host!.StartAsync();

            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            mainWindow.Closed += MainWindow_Closed;

            base.OnStartup(e);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            this.Shutdown();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _log.LogInformation("exiting...");

            (Host?.Services.GetService(typeof(MainWindow)) as IDisposable)?.Dispose();
            (Host?.Services.GetService(typeof(IPlayerModel)) as IDisposable)?.Dispose();

            await Host!.StopAsync();
            base.OnExit(e);
        }

        public void SetTheme(string? theme)
        {
            var themeDic = this.Resources.MergedDictionaries
                            .Where(r => r.Source.ToString()
                            .StartsWith("/themes/"))
                            .FirstOrDefault();

            if (themeDic != null)
            {
                switch (theme?.ToLower())
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
            var control = Current.FindResource(MenuItem.SubmenuHeaderTemplateKey);
            var sw = new System.IO.StringWriter();
            System.Windows.Markup.XamlWriter.Save(control, sw);
            Clipboard.SetText(sw.ToString());
        }
#endif
    }
}

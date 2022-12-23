using Microsoft.Win32;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace miniplayer.lib
{
    public class Spotify
    {
        const string SPOTIFY_APP_URI = "spotify://";
        const string SPOTIFY_WEB_URI = "https://open.spotify.com/";
        public static bool WindowsAppInstalled =>
            Registry.ClassesRoot.OpenSubKey("HKEY_CLASSES_ROOT\\spotify\\shell\\open\\command") != null;

        static Spotify()
        {
            UseApp = WindowsAppInstalled;
        }

        public static bool UseApp { get; private set; }

        public static void Open(Object o = null)
        {
            if (UseApp)
                CreateAppUri(o).CallWithShell();
            else
                CreateWebUri(o).CallWithShell();
        }

        private static Uri CreateWebUri(object o)
            => new Uri(o switch
            {
                FullTrack r => r.Href,
                SimpleTrack r => r.Href,
                FullEpisode r => r.Href,
                SimpleEpisode r => r.Href,
                FullArtist r => r.Href,
                SimpleArtist r => r.Href,
                FullAlbum r => r.Href,
                SimpleAlbum r => r.Href,
                _ => SPOTIFY_WEB_URI
            });

        private static Regex URI_REGEX = new Regex(@"^spotify:(?!/)", RegexOptions.Compiled);
        private static Uri CreateAppUri(object o)
            => new Uri(
                URI_REGEX.Replace(
                    o switch
                    {
                        FullTrack r => r.Uri,
                        SimpleTrack r => r.Uri,
                        FullEpisode r => r.Uri,
                        SimpleEpisode r => r.Uri,
                        FullArtist r => r.Uri,
                        SimpleArtist r => r.Uri,
                        FullAlbum r => r.Uri,
                        SimpleAlbum r => r.Uri,
                        _ => SPOTIFY_APP_URI
                    },
                    "spotify://"));
    }
}

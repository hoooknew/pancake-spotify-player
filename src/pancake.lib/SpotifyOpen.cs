using Microsoft.Win32;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pancake.lib
{
    public class Spotify
    {
        const string SPOTIFY_APP_URI = "spotify://";
        const string SPOTIFY_WEB_URI = "https://open.spotify.com";
        public static bool WindowsAppInstalled =>
            Registry.ClassesRoot.OpenSubKey("spotify\\shell\\open\\command") != null;

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
        {
            (string? type, string? id) = o switch
            {
                LinkableObject r => ( r.Type, r.Id ),
                FullTrack r => ( r.Type.ToString(), r.Id ),
                SimpleTrack r => ( r.Type.ToString(), r.Id ),
                FullEpisode r => ( r.Type.ToString(), r.Id ),
                SimpleEpisode r => ( r.Type.ToString(), r.Id ),
                FullArtist r => ( r.Type, r.Id ),
                SimpleArtist r => ( r.Type, r.Id ),
                FullAlbum r => ( r.Type, r.Id ),
                SimpleAlbum r => ( r.Type, r.Id ),
                _ => (null,  null)
            };

            if (type != null && id != null)
                return new Uri($"{SPOTIFY_WEB_URI}/{type}/{id}");
            else
                return new Uri(SPOTIFY_WEB_URI);
        }

        private static Uri CreateAppUri(object o)
            => new Uri(o switch
            {
                LinkableObject r => r.Uri,
                FullTrack r => r.Uri,
                SimpleTrack r => r.Uri,
                FullEpisode r => r.Uri,
                SimpleEpisode r => r.Uri,
                FullArtist r => r.Uri,
                SimpleArtist r => r.Uri,
                FullAlbum r => r.Uri,
                SimpleAlbum r => r.Uri,
                _ => SPOTIFY_APP_URI
            });
    }
}

using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.lib
{
    public static class Extensions
    {
        public static string? ItemId(this IPlayableItem? item) => item switch { FullTrack f => f.Id, FullEpisode e => e.Id, _ => null };
        public static FullTrack? Track(this CurrentlyPlayingContext? context) => context?.Item as FullTrack;
        public static FullEpisode? Episode(this CurrentlyPlayingContext? context) => context?.Item as FullEpisode;

        public static FullTrack? Track(this IPlayableItem item) => item as FullTrack;
        public static FullEpisode? Episode(this IPlayableItem item) => item as FullEpisode;

        public static void CallWithShell(this Uri uri)
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }

        public static void CallWithShell(string uri)
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        public static TimeSpan MSasTimeSpan(this int ms)
            => new TimeSpan(0, 0, 0, 0, ms);
    }
}

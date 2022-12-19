﻿using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniplayer.lib
{
    public static class Extensions
    {
        public static string? GetItemId(this IPlayableItem? item) => item switch { FullTrack f => f.Id, FullEpisode e => e.Id, _ => null };

        public static FullTrack? GetTrack(this CurrentlyPlayingContext? context) => context?.Item as FullTrack;
        public static FullEpisode? GetEpisode(this CurrentlyPlayingContext? context) => context?.Item as FullEpisode;        
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace miniplayer.ui
{
    public static class PlayerCommands
    {
        public static readonly RoutedCommand SignIn = new RoutedCommand("player_signin", typeof(PlayerCommands));
        public static readonly RoutedCommand Shuffle = new RoutedCommand("player_shuffle", typeof(PlayerCommands));
        public static readonly RoutedCommand SkipPrev = new RoutedCommand("player_skip_prev", typeof(PlayerCommands));
        public static readonly RoutedCommand PlayPause = new RoutedCommand("player_play_pause", typeof(PlayerCommands));
        public static readonly RoutedCommand SkipNext = new RoutedCommand("player_skip_next", typeof(PlayerCommands));
        public static readonly RoutedCommand Repeat = new RoutedCommand("player_repeat", typeof(PlayerCommands));
    }
}

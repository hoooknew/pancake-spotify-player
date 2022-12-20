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
    }
}

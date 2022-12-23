using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace miniplayer.ui
{
    public static class SettingsCommands
    {
        public static readonly RoutedCommand ChangeTheme = new RoutedCommand("settings_change_theme", typeof(SettingsCommands));
        public static readonly RoutedCommand ToggleAlwaysOnTop = new RoutedCommand("settings_toggle_always_on_top", typeof(SettingsCommands));        
    }
}

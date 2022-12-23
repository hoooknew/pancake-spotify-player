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
        public static readonly RoutedCommand HideShowControls = new RoutedCommand("settings_hide_show_controls", typeof(SettingsCommands));
        public static readonly RoutedCommand HideShowProgress = new RoutedCommand("settings_hide_show_progress", typeof(SettingsCommands));
        public static readonly RoutedCommand HideShowTaskbar = new RoutedCommand("settings_hide_show_taskbar", typeof(SettingsCommands));
        public static readonly RoutedCommand SignOut = new RoutedCommand("settings_sign_out", typeof(SettingsCommands));
    }
}

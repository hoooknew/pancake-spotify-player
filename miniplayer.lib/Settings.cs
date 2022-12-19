using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniplayer.lib
{
    public class Settings
    {
        private static readonly string _userSettings = Path.Combine(Constants.LOCAL_APP_DATA, @"settings.json");

        private class _Settings
        {
            public double window_left { get; set; }
            public double window_top { get; set; }
            public double window_width { get; set; }
            public double window_height { get; set; }
        }
    }
}

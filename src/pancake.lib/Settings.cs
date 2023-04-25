using AutoNotify;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.lib
{
    public partial class Settings
    {
        public record Rect(double Left, double Top, double Width, double Height)
        {
            public string Serialize()
                => JsonConvert.SerializeObject(this);

            public static Rect? Deserialize(string text)
                => JsonConvert.DeserializeObject<Rect>(text);            
        }

        private static readonly string _userSettingsPath = Path.Combine(Constants.LOCAL_APP_DATA, @"settings.json");

        private static Settings? __instance;
        public static Settings Instance
        {
            get
            {
                if (__instance == null)
                    lock(typeof(Settings))
                        if (__instance == null)
                        {
                            if (File.Exists(_userSettingsPath))
                                __instance = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(_userSettingsPath));
                            else
                                __instance = new Settings();
                        }

                return __instance!;
            }
        }

        [AutoNotify]
        private string? _mainWindowPlacement = null;

        [AutoNotify]
        private string? _playlistPlacement = null;

        [AutoNotify]
        private string? _theme = "dark";

        [AutoNotify]
        private bool _alwaysOnTop = false;

        [AutoNotify]
        private bool _controlsVisible = true;

        [AutoNotify]
        private bool _progressVisible = true;

        [AutoNotify]
        private bool _taskbarVisible = true;

        [AutoNotify]
        private bool _playlistVisible = true;

        [AutoNotify]
        private double _uiScale = 1.0;

        private static void CreateLocalAppFolder()
        {
            if (!Directory.Exists(Constants.LOCAL_APP_DATA))
                Directory.CreateDirectory(Constants.LOCAL_APP_DATA);
        }        

        public void Save()
        {
            CreateLocalAppFolder();
            File.WriteAllText(_userSettingsPath, JsonConvert.SerializeObject(this));
        }
    }
}

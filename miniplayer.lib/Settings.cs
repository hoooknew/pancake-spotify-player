using AutoNotify;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniplayer.lib
{
    public partial class Settings
    {
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

                return __instance;
            }
        }

        public Settings()
        {
        }

        [AutoNotify]
        private double _windowLeft;
        [AutoNotify]
        private double _windowTop;
        [AutoNotify]
        public double _windowWidth;
        [AutoNotify]
        public double _windowHeight;

        private static void CreateLocalAppFolder()
        {
            if (!Directory.Exists(Constants.LOCAL_APP_DATA))
                Directory.CreateDirectory(Constants.LOCAL_APP_DATA);
        }

        partial void OnPropertyChanged(string propertyName)
        {
            CreateLocalAppFolder();
            File.WriteAllText(_userSettingsPath, JsonConvert.SerializeObject(this));
        }
    }
}

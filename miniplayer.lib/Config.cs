using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniplayer.lib
{
    public static class Config
    {
        private static readonly string _credentialsPath = Path.Combine(Constants.LOCAL_APP_DATA, @"credentials.json");

        private static IConfiguration? __instance = null;
        public static IConfiguration Instance
        {
            get
            {
                if (__instance == null)
                    lock (typeof(Config))
                    {
                        if (__instance == null)
                        {
                            CreateLocalAppFolder();

                            var builder = new ConfigurationBuilder()
                                .AddJsonFile($"appsettings.json", true);
                            __instance = builder.Build();
                        }
                    }

                return __instance;
            }
        }        

        #region app settings
        public static string? ClientId => Instance["clientId"];
        public static int RefreshDelayMS => int.Parse(Instance["refreshDelayMS"] ?? "1000");
        #endregion

        #region credentials
        public static bool TokenAvailable() => File.Exists(_credentialsPath);
        public static void SaveToken(IRefreshableToken? token)
        {
            if (token == null && File.Exists(_credentialsPath))
                File.Delete(_credentialsPath);
            else
                File.WriteAllText(_credentialsPath, JsonConvert.SerializeObject(token));
        }

        private static void CreateLocalAppFolder()
        {            
            if (!Directory.Exists(Constants.LOCAL_APP_DATA))
                Directory.CreateDirectory(Constants.LOCAL_APP_DATA);
        }

        public static IRefreshableToken? LoadToken()
        {
            if (TokenAvailable())
            {
                var json = File.ReadAllText(_credentialsPath);
                return JsonConvert.DeserializeObject<PKCETokenResponse>(json);
            }
            else
                return null;
        }
        #endregion        
    }
}

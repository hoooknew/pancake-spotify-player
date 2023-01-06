using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.lib
{
    public static class Config
    {
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
                            var builder = new ConfigurationBuilder()
                                .AddJsonFile($"appsettings.json", true);
                            __instance = builder.Build();
                        }
                    }

                return __instance;
            }
        }        

        public static string? ClientId => Instance["clientId"];
        public static int RefreshDelayMS => int.Parse(Instance["refreshDelayMS"] ?? "1000");

        public static IConfigurationSection? Logging => Instance.GetSection("Logging");
    }
}

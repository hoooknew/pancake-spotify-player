using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test_auth
{
    internal static class Config
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
        


        private static readonly string _credentialsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"miniplayer\credentials.json");

        public static bool TokenAvailable() => File.Exists(_credentialsPath);

        public static void SaveToken(IRefreshableToken? token)
        {
            if (token == null && File.Exists(_credentialsPath))
                File.Delete(_credentialsPath);
            else
            {
                var directory = Path.GetDirectoryName(_credentialsPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory!);

                File.WriteAllText(_credentialsPath, JsonConvert.SerializeObject(token));
            }
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
    }
}

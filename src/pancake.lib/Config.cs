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
    public interface IConfig
    {
        string? ClientId { get; }
        IConfigurationSection? Logging { get; }
        int RefreshDelayMS { get; }
    }

    public class Config : IConfig
    {
        private readonly IConfiguration _config;


        public Config()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true);

            _config = builder.Build();
        }

        public string? ClientId => _config["clientId"];
        public int RefreshDelayMS => int.Parse(_config["refreshDelayMS"] ?? "1000");

        public IConfigurationSection? Logging => _config.GetSection("Logging");
    }
}

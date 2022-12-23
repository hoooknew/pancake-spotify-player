using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using Swan.Logging;
using static SpotifyAPI.Web.Scopes;
using miniplayer.lib;

namespace test_auth
{
    //https://github.com/JohnnyCrazy/SpotifyAPI-NET/blob/54f8f8960fbd859781fd971efaca94462ca52468/SpotifyAPI.Web.Examples/Example.CLI.PersistentConfig/Program.cs
    /// <summary>
    ///   This is a basic example how to get user access using the Auth package and a CLI Program
    ///   Your spotify app needs to have http://localhost:3000 as redirect uri whitelisted
    /// </summary>
    public class test_auth
    {
        public static async Task Main()
        {
            var uri = new Uri($"spotify:artist:4D8bh9Rvbpq8sHjPWVies5");
        }
    }
}
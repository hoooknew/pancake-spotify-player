using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.lib
{
    public class Logging
    {
        private static readonly ILogger __default;
        private static readonly ILoggerFactory __factory;
        static Logging()
        {
            __factory = LoggerFactory.Create(lb =>
            {                
                var config = Config.Logging;
                if (config != null) 
                    lb.AddConfiguration(config);

                lb.AddDebug();
            });
            __default = __factory.CreateLogger<Logging>();
        }

        public static ILogger Default => __default;
        public static ILoggerFactory Factory => __factory;

        public static ILogger Category(string category) => Factory.CreateLogger(category);
    }
}

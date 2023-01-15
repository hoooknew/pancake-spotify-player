using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.lib
{
    public interface ILogging
    {
        ILogger Default { get; }

        ILogger Create(string category);
        ILogger<T> Create<T>();
    }

    public class Logging : ILogging
    {
        private readonly ILogger _default;
        private readonly ILoggerFactory _factory;
        public Logging(IConfig config)
        {
            _factory = LoggerFactory.Create(lb =>
            {
                if (config.Logging != null)
                    lb.AddConfiguration(config.Logging);

                lb.AddDebug();
            });
            _default = _factory.CreateLogger<Logging>();
        }

        public ILogger Default => _default;
        private ILoggerFactory Factory => _factory;

        public ILogger Create(string category) => Factory.CreateLogger(category);
        public ILogger<T> Create<T>() => Factory.CreateLogger<T>();
    }
}

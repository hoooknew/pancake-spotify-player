using Microsoft.Extensions.Logging;
using pancake.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.tests.lib
{
    public class DebugLogging : ILogging
    {
        private readonly ILogger _default;
        private readonly ILoggerFactory _factory;
        public DebugLogging()
        {
            _factory = LoggerFactory.Create(lb =>
            {
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

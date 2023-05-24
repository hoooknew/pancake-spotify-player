using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace pancake.ui
{
    public interface IDispatchProvider
    {
        void Invoke(Action callback);
        void BeginInvoke(Action callback);
    }

    internal class DispatchProvider : IDispatchProvider
    {
        private Dispatcher _dispatcher;

        public DispatchProvider(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Invoke(Action callback)
        {
            _dispatcher.Invoke(callback);
        }

        public void BeginInvoke(Action callback)
        {
            _dispatcher.BeginInvoke(callback);
        }
    }
}

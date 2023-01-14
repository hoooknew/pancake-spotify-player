using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace pancake.models
{
    public interface IDispatcherHelper
    {
        void Invoke(Action callback);
        TResult Invoke<TResult>(Func<TResult> callback);
    }
    internal class DispatcherHelper : IDispatcherHelper
    {
        private readonly Dispatcher _dispatcher;
        public DispatcherHelper(App app) 
        {
            this._dispatcher = app.Dispatcher;
        }
        public void Invoke(Action callback) 
            => _dispatcher.Invoke(callback);
        public TResult Invoke<TResult>(Func<TResult> callback) 
            => _dispatcher.Invoke<TResult>(callback);
    }
}

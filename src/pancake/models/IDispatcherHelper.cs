using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.models
{
    public interface IDispatcherHelper
    {
        void Invoke(Action callback);
        TResult Invoke<TResult>(Func<TResult> callback);
    }
}

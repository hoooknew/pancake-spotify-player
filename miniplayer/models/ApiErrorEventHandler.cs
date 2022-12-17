﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniplayer.models
{
    internal class ApiErrorEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }
        public ApiErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class AutoNotifyAttribute : Attribute
    {
        public string PropertyName { get; set; }
    }
}

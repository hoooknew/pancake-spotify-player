using AutoNotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute(string? propertyName = null)
        {
            this.PropertyName = propertyName;        
        }

        public string? PropertyName { get; set; }
    }
}

namespace miniplayer.lib
{
    public partial class Settings
    {
        private static readonly string _userSettings = Path.Combine(Constants.LOCAL_APP_DATA, @"settings.json");

        [AutoNotify("WindowLeft")]
        public double _window_left;
        [AutoNotify("WindowTop")]
        public double _window_top;
        //[AutoNotify]
        //public double _window_width;
        //[AutoNotify]
        //public double _window_height;
    }
}

﻿using AutoNotify;
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
        public string? PropertyName { get; set; }
    }
}

namespace miniplayer.lib
{
    public partial class Settings
    {
        private static readonly string _userSettings = Path.Combine(Constants.LOCAL_APP_DATA, @"settings.json");

        public Settings()
        {            
        }

        [AutoNotify]
        private double _windowLeft;
        [AutoNotify]
        private double _windowTop;
        [AutoNotify]
        public double _windowWidth;
        [AutoNotify]
        public double _windowHeight;
    }
}

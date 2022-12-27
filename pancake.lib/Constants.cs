using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.lib
{
    internal class Constants
    {
        internal static readonly string LOCAL_APP_DATA = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"pancake");
    }
}

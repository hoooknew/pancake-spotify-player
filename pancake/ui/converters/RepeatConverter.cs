using pancake.models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace pancake.ui.converters
{
    internal class RepeatConverter : IValueConverter
    {
        public Geometry? Off { get; set; }
        public Geometry? Track { get; set; }
        public Geometry? Context { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch(value as string)
            {                
                case "track":
                    return Track!;
                case "context":
                    return Context!;
                default:
                case "off":
                    return Off!;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

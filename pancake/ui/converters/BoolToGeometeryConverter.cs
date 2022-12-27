using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace pancake.ui.converters
{
    internal class BoolToGeometeryConverter : IValueConverter
    {
        public Geometry? TrueValue { get; set; } = null;
        public Geometry? FalseValue { get; set; } = null;

        public Geometry? NullValue { get; set; } = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? this.TrueValue! : this.FalseValue!;
            else if (this.NullValue != null)
                return this.NullValue;
            else
                throw new ArgumentException("bound value should be a boolean");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

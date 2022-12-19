using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace miniplayer.ui.converters
{
    internal class MStoStringConverter : IValueConverter
    {
        public string? MinutesFormat { get; set; }
        public string? HoursFormat { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
            {
                var ts = new TimeSpan(0, 0, 0, 0, i);

                if (ts.Hours > 0)
                    return ts.ToString(this.HoursFormat ?? this.MinutesFormat);
                else
                    return ts.ToString(this.MinutesFormat ?? this.HoursFormat);
            }
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

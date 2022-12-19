using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace miniplayer.ui.converters
{
    internal class TextBoxsToMaxSizeMultiConverter : IMultiValueConverter
    {

        public int Padding { get; set; } = 0;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.OfType<TextBlock>()
                .Select(r => MeasureString(r))
                .Select(r => r.Width)
                .Max() + Padding;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private Size MeasureString(TextBlock textBlock)
        {            
            var formattedText = new FormattedText(
                textBlock.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1);

            return new Size(formattedText.Width, formattedText.Height);
        }
    }
}

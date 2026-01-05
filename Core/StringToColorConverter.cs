using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WassControlSys.Core
{
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                try
                {
                    return System.Windows.Media.ColorConverter.ConvertFromString(s);
                }
                catch
                {
                    return Colors.Transparent;
                }
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

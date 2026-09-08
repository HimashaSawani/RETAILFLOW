using System;
using System.Globalization;
using System.Windows.Data;

namespace RetailFlow.Helpers;

public class BarHeightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values != null && values.Length > 0 && values[0] is double ratio)
        {
            const double maxHeight = 130.0;
            return Math.Max(12.0, ratio * maxHeight);
        }
        return 12.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

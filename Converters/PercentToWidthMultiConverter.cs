using System.Globalization;
using System.Windows.Data;

namespace TopBar.Converters;

public sealed class PercentToWidthMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not double percent || values[1] is not double totalWidth)
            return 0d;

        var clamped = Math.Clamp(percent, 0, 100);
        return totalWidth * (clamped / 100.0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

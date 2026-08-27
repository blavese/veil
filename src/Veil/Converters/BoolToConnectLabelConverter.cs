using System.Globalization;
using System.Windows.Data;

namespace Veil.Converters;

public class BoolToConnectLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Disconnect" : "Connect";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

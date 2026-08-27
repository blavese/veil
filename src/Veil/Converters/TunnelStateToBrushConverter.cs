using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Veil.Services;

namespace Veil.Converters;

public class TunnelStateToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            TunnelState.Connected => new SolidColorBrush(Color.FromRgb(0x4F, 0xD6, 0x81)),
            TunnelState.Connecting => new SolidColorBrush(Color.FromRgb(0xE0, 0xB4, 0x4A)),
            TunnelState.Disconnecting => new SolidColorBrush(Color.FromRgb(0xE0, 0xB4, 0x4A)),
            TunnelState.Error => new SolidColorBrush(Color.FromRgb(0xE5, 0x59, 0x5A)),
            _ => new SolidColorBrush(Color.FromRgb(0x7A, 0x82, 0x90)),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

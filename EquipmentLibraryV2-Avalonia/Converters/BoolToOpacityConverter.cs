using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EquipmentLibraryV2_Avalonia.Converters;

public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 0.0 : 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is 0.0;
    }
}
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EquipmentLibraryV2_Avalonia.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => value is true ? new SolidColorBrush(Color.Parse("#4CAF50")) : new SolidColorBrush(Color.Parse("#D32F2F"));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
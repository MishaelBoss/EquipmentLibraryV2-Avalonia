using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EquipmentLibraryV2_Avalonia.Converters;

public class DoubleToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new GridLength((double)(value ?? 60));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
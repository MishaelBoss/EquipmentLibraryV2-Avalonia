namespace EquipmentLibraryV2_Avalonia.Helpers;

public static class PluralizeHelper
{
    public static string ToQuantity(this int count, string one, string few, string many)
    {
        var abs = Math.Abs(count) % 100;
        var last = abs % 10;
        
        if (abs is > 10 and < 20)
            return $"{count} {many}";

        if (last is > 1 and < 5)
            return $"{count} {few}";

        return last == 1 ? $"{count} {one}" : $"{count} {many}";
    }
}
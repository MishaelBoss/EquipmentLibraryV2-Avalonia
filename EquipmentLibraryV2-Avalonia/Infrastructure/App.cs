namespace EquipmentLibraryV2_Avalonia.Infrastructure;

public abstract class App
{
    public static bool IsDeveloperMode
    {
        get
        {
#if DEBUG
            return true;
#else
                return false;
#endif
        }
    }
}
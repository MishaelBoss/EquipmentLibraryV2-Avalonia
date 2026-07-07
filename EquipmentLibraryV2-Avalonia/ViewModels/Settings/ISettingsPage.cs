namespace EquipmentLibraryV2_Avalonia.ViewModels.Settings;

public interface ISettingsPage
{
    bool HasChanges { get; }
    void Save();
}

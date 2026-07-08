namespace EquipmentLibraryV2_Avalonia.Services.Interfaces;

internal interface ISettingsPage
{
    bool HasChanges { get; }
    void Save();
}

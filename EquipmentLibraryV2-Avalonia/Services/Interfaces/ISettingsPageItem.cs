using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia.Services.Interfaces;

internal interface ISettingsPageItem
{
    public string Title { get; set; }
    public ViewModelBase ViewModel { get; set; }
}

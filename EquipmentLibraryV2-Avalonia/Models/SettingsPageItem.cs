using EquipmentLibraryV2_Avalonia.Services.Interfaces;
using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia.Models;

public class SettingsPageItem(string title, ViewModelBase viewModelBase) : ISettingsPageItem
{
    public string Title { get; set; } = title;
    public ViewModelBase ViewModel { get; set; } = viewModelBase;
}

using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Services.Interfaces;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Settings;

public partial class UpdatesUserControlViewModel : ViewModelBase, ISettingsPage
{
    private readonly AppSettings _settings;
    private readonly bool _originalValue;

    [ObservableProperty] public partial bool CheckLatestUpdates { get; set; }

    public bool HasChanges => _originalValue != CheckLatestUpdates;

    public UpdatesUserControlViewModel()
    {
        _settings = AppSettings.Load();
        _originalValue = _settings.CheckLatestUpdates;
        CheckLatestUpdates = _settings.CheckLatestUpdates;
    }

    public void Save()
    {
        _settings.CheckLatestUpdates = CheckLatestUpdates;
        _settings.Save();
    }
}

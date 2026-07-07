using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentLibraryV2_Avalonia.Infrastructure;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Settings;

public partial class UpdatesUserControlViewModel : ViewModelBase
{
    private readonly AppSettings _settings;

    [ObservableProperty] public partial bool CheckLatestUpdates { get; set; }

    public UpdatesUserControlViewModel()
    {
        _settings = AppSettings.Load();
        CheckLatestUpdates = _settings.CheckLatestUpdates;
    }
    
    partial void OnCheckLatestUpdatesChanged(bool value)
    {
        _settings.CheckLatestUpdates = value;
        _settings.Save();
    }
}
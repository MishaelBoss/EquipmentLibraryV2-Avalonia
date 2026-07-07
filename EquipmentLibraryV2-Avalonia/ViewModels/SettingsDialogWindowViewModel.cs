using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentLibraryV2_Avalonia.ViewModels.Settings;

namespace EquipmentLibraryV2_Avalonia.ViewModels;

public class SettingsPageItem
{
    public string Title { get; set; } = string.Empty;
    // ReSharper disable once NullableWarningSuppressionIsUsed
    public ViewModelBase ViewModel { get; set; } = null!;
}

public partial class SettingsDialogWindowViewModel : ViewModelBase
{
    public ObservableCollection<SettingsPageItem> Pages { get; }

    [ObservableProperty] public partial SettingsPageItem? SelectedPage { get; set; }

    public SettingsDialogWindowViewModel()
    {
        Pages =
        [
            new SettingsPageItem { Title = "Логирование", ViewModel = new LoggingViewModel() },
            new SettingsPageItem { Title = "Обновления", ViewModel = new UpdatesUserControlViewModel() }
        ];

        SelectedPage = Pages[0];
    }
    
    [RelayCommand]
    public void Cancel(Window? window) {
        window?.Close();
    }

    [RelayCommand]
    public void Apply() { 
    }

    [RelayCommand]
    public void Ok(Window? window) {
        window?.Close();
    }
}

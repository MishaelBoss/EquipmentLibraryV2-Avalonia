using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

    [ObservableProperty] public partial bool HasChanges { get; set; }

    public SettingsDialogWindowViewModel()
    {
        Pages =
        [
            new SettingsPageItem { Title = "Логирование", ViewModel = new LoggingViewModel() },
            new SettingsPageItem { Title = "Обновления", ViewModel = new UpdatesUserControlViewModel() }
        ];

        SelectedPage = Pages[0];

        foreach (var page in Pages)
        {
            if (page.ViewModel is INotifyPropertyChanged npc)
                npc.PropertyChanged += OnPagePropertyChanged;
        }
    }

    private void OnPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RecalculateHasChanges();
    }

    private void RecalculateHasChanges()
    {
        foreach (var page in Pages)
        {
            if (page.ViewModel is not ISettingsPage { HasChanges: true }) continue;
            HasChanges = true;
            return;
        }
        HasChanges = false;
    }

    [RelayCommand]
    public void Cancel(Window? window)
    {
        window?.Close();
    }

    [RelayCommand]
    public void Apply()
    {
        foreach (var page in Pages)
        {
            if (page.ViewModel is ISettingsPage sp)
                sp.Save();
        }
        RecalculateHasChanges();
    }

    [RelayCommand]
    public void Ok(Window? window)
    {
        Apply();
        window?.Close();
    }
}

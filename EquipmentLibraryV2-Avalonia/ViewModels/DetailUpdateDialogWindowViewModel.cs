using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Models;
using Serilog;

namespace EquipmentLibraryV2_Avalonia.ViewModels;

public partial class DetailUpdateDialogWindowViewModel : ViewModelBase
{
    [ObservableProperty] public partial string NewVersion { get; set; }
    [ObservableProperty] public partial string CurrentVersion { get; set; }
    [ObservableProperty] public partial string ReleaseDate { get; set; }
    [ObservableProperty] public partial string ReleaseNotes { get; set; }
    [ObservableProperty] public partial string UpdateUrl { get; set; } = "https://github.com/MishaelBoss/EquipmentLibraryV2-Avalonia/releases";

    public bool IsConfirmed { get; private set; } = false;
    
    [RelayCommand]
    public void Close(Window? window)
    {
        window?.Close();
    }

    [RelayCommand]
    public async Task OpenAllChanges(Visual visualContext)
    {
        if (string.IsNullOrWhiteSpace(UpdateUrl))
        {
            Log.Warning("Attempted to open an update link, but the URL is empty or contains only spaces.");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(visualContext);

        if (topLevel?.Launcher is { } launcher)
        {
            var success = await launcher.LaunchUriAsync(new Uri(UpdateUrl));
            if (!success)
            {
                Log.Error("The operating system was unable to open the URL in the browser: {UpdateUrl}", UpdateUrl);
            }
            else
            {
                Log.Information("The update link has been successfully opened in the browser. URL: {UpdateUrl}", UpdateUrl);
            }
        }
        else
        {
            Log.Warning("Unable to get TopLevel or ILauncher to open the link. Passed visual context: {ContextType}", 
                visualContext.GetType().Name);
        }
    }

    public DetailUpdateDialogWindowViewModel(DetailUpdateDialog data)
    {
        CurrentVersion = AppConfig.DisplayVersion;
        NewVersion = data.NewVersion;
        ReleaseNotes = data.ReleaseNotes;
        ReleaseDate = data.ReleaseDate;
    }
}

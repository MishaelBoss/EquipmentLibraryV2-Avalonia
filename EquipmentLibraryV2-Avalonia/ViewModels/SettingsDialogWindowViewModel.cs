using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using System.Diagnostics;
using System.IO;

namespace EquipmentLibraryV2_Avalonia.ViewModels;

public partial class SettingsDialogWindowViewModel : ViewModelBase
{
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

    [RelayCommand]
    public void OpenLogsFolder()
    {
        string logsPath = Path.Combine(AppPaths.UserDataDir, "logs");

        if (!Directory.Exists(logsPath))
        {
            Directory.CreateDirectory(logsPath);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = logsPath,
            UseShellExecute = true,
            Verb = "open"
        });
    }
}

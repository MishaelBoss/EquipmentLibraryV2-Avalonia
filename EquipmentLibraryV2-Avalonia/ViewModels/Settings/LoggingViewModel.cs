using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using EquipmentLibraryV2_Avalonia.Infrastructure;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Settings;

public partial class LoggingViewModel : ViewModelBase
{
    [RelayCommand]
    public void OpenLogsFolder()
    {
        var logsPath = Path.Combine(AppPaths.UserDataDir, "logs");

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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class ErrorUserControlViewModel : ViewModelBase
{
    public static ErrorUserControlViewModel Instance { get; } = new();

    [ObservableProperty] public partial NotificationType type { get; set; }
    [ObservableProperty] public partial object Object { get; set; }
    [ObservableProperty] public partial bool IsClosing { get; set; }

    [RelayCommand]
    public async Task Close()
    {
        IsClosing = true;
        await Task.Delay(300);
        WeakReferenceMessenger.Default.Send(new ShowOrHideNotification(ErrorAction.Remove, this));
    }
}
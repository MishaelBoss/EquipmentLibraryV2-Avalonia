using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class NotificationUserControlViewModel : ViewModelBase
{
    [RelayCommand]
    public void Hide() 
        => WeakReferenceMessenger.Default.Send(new OpenOrCloseNotificationMessage());
}
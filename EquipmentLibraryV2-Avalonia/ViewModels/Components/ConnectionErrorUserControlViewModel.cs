using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class ConnectionErrorUserControlViewModel : ViewModelBase
{
    public static ConnectionErrorUserControlViewModel Instance { get; } = new();
    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new ShowOrHideError(ErrorAction.Remove, this));
    }
}
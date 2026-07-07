using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class ConnectionErrorUserControlViewModel : ViewModelBase
{
    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new ShowOrHideError());
    }
}
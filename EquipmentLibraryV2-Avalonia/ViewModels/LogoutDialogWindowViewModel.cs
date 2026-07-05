using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using EquipmentLibraryV2_Avalonia.Services;

namespace EquipmentLibraryV2_Avalonia.ViewModels;

public partial class LogoutDialogWindowViewModel : ViewModelBase
{
    [RelayCommand]
    public void Close(Window? window)
    {
        window?.Close();
    }

    [RelayCommand]
    public void Logout(Window? window)
    {
        AuthService.Logout();

        window?.Close();
    }
}

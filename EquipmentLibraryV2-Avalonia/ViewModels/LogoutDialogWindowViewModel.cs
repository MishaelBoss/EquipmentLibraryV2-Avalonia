using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.Services;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;

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
        var login = AuthService.CurrentSession?.Login;

        AuthService.Logout();

        WeakReferenceMessenger.Default.Send(
            new ShowOrHideNotification(
                ErrorAction.Add,
                new MessageUserControlViewModel
                {
                    Type = NotificationType.Info,
                    Title = "Вы вышли из аккаунта",
                    Object = login is null ? "Сеанс завершён." : $"Пользователь: {login}"
                }));

        window?.Close();
    }
}

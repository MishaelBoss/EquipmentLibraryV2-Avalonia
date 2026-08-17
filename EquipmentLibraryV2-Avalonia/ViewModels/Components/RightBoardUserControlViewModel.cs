using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Services;
using System.Collections.ObjectModel;
using Avalonia.Svg.Skia;
using EquipmentLibraryV2_Avalonia.Views;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class RightBoardUserControlViewModel : ViewModelBase, IRecipient<LoginMessage>, IRecipient<LogoutMessage>
{
    [ObservableProperty] public partial ObservableCollection<DashboardButtonViewModel> Buttons { get; set; } = [];

    public RightBoardUserControlViewModel() 
    {
        WeakReferenceMessenger.Default.RegisterAll(this);

        UpdateUi();
    }

    public void Receive(LoginMessage message) 
    {
        UpdateUi();
    }

    public void Receive(LogoutMessage message)
    {
        UpdateUi();
    }

    [RelayCommand]
    public void OpenAdminPanel()
    {
        WeakReferenceMessenger.Default.Send(new OpenAdminPanelMessage());
    }

    [RelayCommand]
    public void OpenAnalytics()
    {
        WeakReferenceMessenger.Default.Send(new OpenAnalyticsMessage());
    }

    [RelayCommand]
    public void OpenMeasurementRegister()
    {
        WeakReferenceMessenger.Default.Send(new OpenMeasurementRegisterMessage());
    }

    [RelayCommand]
    public void OpenRegisterOfTestingEquipment()
    {
        WeakReferenceMessenger.Default.Send(new OpenRegisterOfTestingEquipmentMessage());
    }

    [RelayCommand]
    public void OpenLibrary() 
    {
        WeakReferenceMessenger.Default.Send(new OpenLibraryMessage());
    }

    [RelayCommand]
    public void OpenWorkArea()
    {
        WeakReferenceMessenger.Default.Send(new OpenWorkAreaMessage());
    }

    [RelayCommand]
    public async Task OpenProfileOrAuthorization()
    {
        if (await AuthService.TryAutoLoginAsync())
        {
            // WeakReferenceMessenger.Default.Send(new OpenOrCloseProfileMessage());

            var dialog = new LogoutDialogWindow();

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);
                }
            }
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseAuthorizationMessage());
        }
    }

    [RelayCommand]
    public void OpenSettings()
    {
        var dialog = new SettingsDialogWindow();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;

            if (mainWindow != null)
            {
                dialog.ShowDialog(mainWindow);
            }
        }
    }

    private async void UpdateUi()
    {
        await AuthService.TryAutoLoginAsync();

        Buttons.Clear();

        var roleId = AuthService.CurrentSession?.UserRole ?? 0;

        var newButtons = new List<DashboardButtonViewModel>
        {
            new("Admin panel", OpenAdminPanelCommand, "shield-check.svg", () => roleId == 1),
            new("Work area", OpenWorkAreaCommand, "grid-2x2.svg",() => roleId is 1 or 2),
            new("Аналитика", OpenAnalyticsCommand, "chart-pie.svg", () => true),
            new("СИ (Средства измерений)", OpenMeasurementRegisterCommand, "library-big.svg", () => roleId is 1 or 2),
            new("ИО (Испытательное оборудование)", OpenRegisterOfTestingEquipmentCommand, "library-big.svg", () => roleId is 1 or 2),
            new("Вся библиотека", OpenLibraryCommand, "library-big.svg", () => true),
        };

        var visibleButtons = newButtons.Where(b => b.IsButtonVisible);

        foreach (var btn in visibleButtons)
        {
            Buttons.Add(btn);
        }
    }

    ~RightBoardUserControlViewModel() 
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}

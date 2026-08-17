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
using Serilog;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class RightBoardUserControlViewModel : ViewModelBase, IRecipient<LoginMessage>, IRecipient<LogoutMessage>, IDisposable
{
    [ObservableProperty] public partial ObservableCollection<DashboardButtonViewModel> Buttons { get; set; } = [];

    public RightBoardUserControlViewModel() 
    {
        Log.Information("Starting dashboard buttons initialization.");
        
        IsActive = true;
        
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

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        var mainWindow = desktop.MainWindow;

        if (mainWindow != null)
        {
            dialog.ShowDialog(mainWindow);
        }
    }

    private async void UpdateUi()
    {
        try
        {
            await AuthService.TryAutoLoginAsync();

            Buttons.Clear();

            var roleId = AuthService.CurrentSession?.UserRole ?? 0;
            
            var newButtons = new List<DashboardButtonViewModel>
            {
                new("Админ панель", OpenAdminPanelCommand, "shield-check.svg", () => roleId == 1),
                new("Рабочие место", OpenWorkAreaCommand, "grid-2x2.svg",() => roleId is 1 or 2),
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
            
            Log.Information("Dashboard successfully initialized. Total visible buttons added: {Count}", Buttons.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Critical error during dashboard initialization or auto-login process.");
        }
    }

    public void Dispose()
    {
        IsActive = false;
        
        GC.SuppressFinalize(this);
    }
}

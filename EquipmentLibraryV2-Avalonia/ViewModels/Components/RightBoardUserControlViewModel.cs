using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Scripts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class RightBoardUserControlViewModel : ViewModelBase, IRecipient<LoginMessage>
{
    [ObservableProperty] public partial ObservableCollection<DashboardButtonViewModel> Buttons { get; set; } = [];

    public RightBoardUserControlViewModel() 
    {
        WeakReferenceMessenger.Default.Register(this);

        UpdateUi();
    }

    public void Receive(LoginMessage message) 
    {
        UpdateUi();
    }

    [RelayCommand]
    public void OpenAdminPanel()
    {
        WeakReferenceMessenger.Default.Send(new OpenAdminPanelMessage());
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
            WeakReferenceMessenger.Default.Send(new OpenOrCloseProfileMessage());
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseAuthorizationMessage());
        }
    }

    [RelayCommand]
    public void OpenSettings()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseSettingsMessage());
    }

    private async void UpdateUi()
    {
        await AuthService.TryAutoLoginAsync();

        Buttons.Clear();

        var roleId = AuthService.CurrentSession?.UserRole ?? 0;

        var newButtons = new List<DashboardButtonViewModel>
        {
            new("Admin panel",OpenAdminPanelCommand,  LoadBitmap("avares://EquipmentLibraryV2_Avalonia/Assets/admin-panel-64.png"), () => roleId == 1),
            new("Work area", OpenWorkAreaCommand, LoadBitmap("avares://EquipmentLibraryV2_Avalonia/Assets/library-64.png"),() => roleId is 1 or 2),
            new("Measurement register", OpenMeasurementRegisterCommand, LoadBitmap("avares://EquipmentLibraryV2_Avalonia/Assets/library-64.png"), () => roleId is 1 or 2),
            new("Register of testing equipment", OpenRegisterOfTestingEquipmentCommand, LoadBitmap("avares://EquipmentLibraryV2_Avalonia/Assets/library-64.png"), () => roleId is 1 or 2),
            new("Library", OpenLibraryCommand, LoadBitmap("avares://EquipmentLibraryV2_Avalonia/Assets/library-64.png"), () => true),
        };

        var visibleButtons = newButtons.Where(b => b.IsVisible);

        foreach (var btn in visibleButtons)
        {
            Buttons.Add(btn);
        }
    }

    private static Bitmap LoadBitmap(string uriString)
    {
        using var stream = AssetLoader.Open(new Uri(uriString));
        return new Bitmap(stream);
    }

    ~RightBoardUserControlViewModel() 
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}

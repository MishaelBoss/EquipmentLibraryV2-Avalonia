using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;

namespace EquipmentLibraryV2_Avalonia.Views.Components;

public partial class AuthorizationUserControlView : UserControl
{
    public AuthorizationUserControlView()
    {
        InitializeComponent();
    }

    private void CloseAuthorization_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed) 
            WeakReferenceMessenger.Default.Send(new OpenOrCloseAuthorizationMessage());
    }
    
    private void BlockPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }
}
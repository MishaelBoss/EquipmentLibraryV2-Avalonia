using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia.Views;

public partial class LogoutDialogWindow : CustomChromeWindow
{
    public LogoutDialogWindow()
    {
        InitializeComponent();
        DataContext = new LogoutDialogWindowViewModel();
        WireCaptionButtons(
            this.FindControl<Button>("MinimizeButton"),
            null,
            this.FindControl<Button>("CloseButton"));
    }
}
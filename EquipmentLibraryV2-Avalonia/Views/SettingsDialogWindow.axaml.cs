using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels;
using EquipmentLibraryV2_Avalonia.Views;

namespace EquipmentLibraryV2_Avalonia;

public partial class SettingsDialogWindow : CustomChromeWindow
{
    public SettingsDialogWindow()
    {
        InitializeComponent();
        DataContext = new SettingsDialogWindowViewModel();
        WireCaptionButtons(
            this.FindControl<Button>("MinimizeButton"),
            null,
            this.FindControl<Button>("CloseButton"));
    }
}
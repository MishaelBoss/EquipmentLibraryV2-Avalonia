using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia.Views;

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
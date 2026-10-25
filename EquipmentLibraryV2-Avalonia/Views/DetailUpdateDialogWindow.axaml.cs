using Avalonia.Controls;

namespace EquipmentLibraryV2_Avalonia.Views;

public partial class DetailUpdateDialogWindow : CustomChromeWindow
{
    public DetailUpdateDialogWindow()
    {
        InitializeComponent();
        WireCaptionButtons(
            this.FindControl<Button>("MinimizeButton"),
            null,
            this.FindControl<Button>("CloseButton"));
    }
}
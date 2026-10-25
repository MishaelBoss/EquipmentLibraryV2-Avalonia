using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia.Views;

public partial class CreateAdminDialogWindow : CustomChromeWindow
{
    public CreateAdminDialogWindow()
    {
        InitializeComponent();
        DataContext = new CreateAdminDialogWindowViewModel();
        WireCaptionButtons(
            this.FindControl<Button>("MinimizeButton"),
            null,
            this.FindControl<Button>("CloseButton"));
    }
}
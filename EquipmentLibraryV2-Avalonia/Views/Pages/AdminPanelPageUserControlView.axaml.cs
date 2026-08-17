using Avalonia;
using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels.Pages;

namespace EquipmentLibraryV2_Avalonia.Views.Pages;

public partial class AdminPanelPageUserControlView : UserControl
{
    public AdminPanelPageUserControlView()
    {
        InitializeComponent();
        DataContext = new AdminPanelPageUserControlViewModel();
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (DataContext is IDisposable disposable)
            disposable.Dispose();
    }
}
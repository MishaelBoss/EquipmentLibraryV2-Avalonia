using Avalonia;
using Avalonia.Controls;

namespace EquipmentLibraryV2_Avalonia.Views.Pages;

public partial class LibraryPageUserControlView : UserControl
{
    public LibraryPageUserControlView()
    {
        InitializeComponent();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        if (DataContext is IDisposable disposable)
            disposable.Dispose();
    }
}
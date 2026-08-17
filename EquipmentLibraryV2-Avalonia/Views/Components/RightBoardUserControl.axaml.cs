using Avalonia;
using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;

namespace EquipmentLibraryV2_Avalonia.Views.Components;

public partial class RightBoardUserControl : UserControl
{
    public RightBoardUserControl()
    {
        InitializeComponent();
        DataContext = new RightBoardUserControlViewModel();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        if (DataContext is IDisposable disposable)
            disposable.Dispose();
    }
}
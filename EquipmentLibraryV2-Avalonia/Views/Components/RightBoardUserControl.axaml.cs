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
}
using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels.Pages;

namespace EquipmentLibraryV2_Avalonia.Views.Pages;

public partial class WorkAreaUserControlView : UserControl
{
    public WorkAreaUserControlView()
    {
        InitializeComponent();
        DataContext = new WorkAreaUserControlViewModel();
    }
}
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
}
using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels.Pages;

namespace EquipmentLibraryV2_Avalonia.Views.Pages;

public partial class AnalyticsPageUserControlView : UserControl
{
    public AnalyticsPageUserControlView()
    {
        InitializeComponent();
        DataContext = new AnalyticsPageUserControlViewModel();
    }
}
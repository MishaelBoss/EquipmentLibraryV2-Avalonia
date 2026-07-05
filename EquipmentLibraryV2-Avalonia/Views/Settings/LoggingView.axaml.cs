using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels.Settings;

namespace EquipmentLibraryV2_Avalonia.Views.Settings;

public partial class LoggingView : UserControl
{
    public LoggingView()
    {
        InitializeComponent();
        DataContext = new LoggingViewModel();
    }
}
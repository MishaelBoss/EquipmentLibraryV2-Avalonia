using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels.Pages;

namespace EquipmentLibraryV2_Avalonia.Views.Pages;

public partial class MeasurementRegisterPageUserControlView : UserControl
{
    public MeasurementRegisterPageUserControlView()
    {
        InitializeComponent();
        DataContext = new MeasurementRegisterPageUserControlViewModel();
    }
}
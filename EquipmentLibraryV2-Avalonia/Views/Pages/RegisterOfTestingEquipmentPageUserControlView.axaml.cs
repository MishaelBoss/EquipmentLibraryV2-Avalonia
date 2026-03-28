using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels.Pages;

namespace EquipmentLibraryV2_Avalonia.Views.Pages;

public partial class RegisterOfTestingEquipmentPageUserControlView : UserControl
{
    public RegisterOfTestingEquipmentPageUserControlView()
    {
        InitializeComponent();
        DataContext = new RegisterOfTestingEquipmentPageUserControlViewModel();
    }
}
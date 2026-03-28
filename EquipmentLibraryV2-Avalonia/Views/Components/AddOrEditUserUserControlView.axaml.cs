using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;

namespace EquipmentLibraryV2_Avalonia.Views.Components;

public partial class AddOrEditUserUserControlView : UserControl
{
    public AddOrEditUserUserControlView()
    {
        InitializeComponent();
        DataContext = new AddOrEditUserUserControlViewModel();
    }
}
using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels.Settings;

namespace EquipmentLibraryV2_Avalonia.Views.Settings;

public partial class UpdatesUserControlView : UserControl
{
    public UpdatesUserControlView()
    {
        InitializeComponent();
        DataContext = new UpdatesUserControlViewModel();
    }
}
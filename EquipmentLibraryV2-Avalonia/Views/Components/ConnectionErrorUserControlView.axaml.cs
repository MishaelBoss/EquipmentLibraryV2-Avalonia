using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;

namespace EquipmentLibraryV2_Avalonia.Views.Components;

public partial class ConnectionErrorUserControlView : UserControl
{
    public ConnectionErrorUserControlView()
    {
        InitializeComponent();
        DataContext = new ConnectionErrorUserControlViewModel();
    }
}
using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia.Views;

public partial class AboutDialogWindow : Window
{
    public AboutDialogWindow()
    {
        InitializeComponent();
        DataContext = new AboutDialogWindowViewModel();
    }
}
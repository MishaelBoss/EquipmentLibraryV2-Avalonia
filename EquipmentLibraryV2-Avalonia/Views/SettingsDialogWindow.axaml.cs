using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia;

public partial class SettingsDialogWindow : Window
{
    public SettingsDialogWindow()
    {
        InitializeComponent();
        DataContext = new SettingsDialogWindowViewModel();
    }
}
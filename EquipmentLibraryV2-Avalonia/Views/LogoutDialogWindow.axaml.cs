using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia;

public partial class LogoutDialogWindow : Window
{
    public LogoutDialogWindow()
    {
        InitializeComponent();
        DataContext = new LogoutDialogWindowViewModel();
    }
}
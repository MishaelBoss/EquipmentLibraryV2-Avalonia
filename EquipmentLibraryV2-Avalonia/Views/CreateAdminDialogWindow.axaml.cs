using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia.Views;

public partial class CreateAdminDialogWindow : Window
{
    public CreateAdminDialogWindow()
    {
        InitializeComponent();
        DataContext = new CreateAdminDialogWindowViewModel();
    }
}
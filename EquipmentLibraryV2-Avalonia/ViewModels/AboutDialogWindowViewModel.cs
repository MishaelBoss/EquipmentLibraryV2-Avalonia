using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentLibraryV2_Avalonia.ViewModels;

public partial class AboutDialogWindowViewModel : ViewModelBase
{
    [RelayCommand]
    public void Close(Window? window)
    {
        window?.Close();
    }
}
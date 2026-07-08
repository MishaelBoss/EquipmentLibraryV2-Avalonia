using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class DashboardButtonViewModel : ViewModelBase
{
    [ObservableProperty] public partial string ButtonText { get; set; }

    [ObservableProperty] public partial bool IsButtonVisible { get; set; } = true;

    public ICommand Command { get; }
    public Bitmap IconPath { get; }

    private readonly Func<bool> _isVisibleFunc;

    public DashboardButtonViewModel(string buttonText, ICommand command, Bitmap iconPath, Func<bool>? isVisibleFunc = null)
    {
        ButtonText = buttonText;
        Command = command;
        IconPath = iconPath;
        _isVisibleFunc = isVisibleFunc ?? (() => true);

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        IsButtonVisible = _isVisibleFunc();
    }
}

using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Input;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class DashboardButtonViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _content;

    [ObservableProperty]
    private bool _isVisible = true;

    public ICommand Command { get; }
    public Bitmap IconPath { get; }

    private readonly Func<bool> _isVisibleFunc;

    public DashboardButtonViewModel(string content, ICommand command, Bitmap iconPath, Func<bool>? isVisibleFunc = null)
    {
        Content = content;
        Command = command;
        IconPath = iconPath;
        _isVisibleFunc = isVisibleFunc ?? (() => true);

        UpdateVisibility();
    }

    public void UpdateVisibility()
    {
        IsVisible = _isVisibleFunc();
    }
}

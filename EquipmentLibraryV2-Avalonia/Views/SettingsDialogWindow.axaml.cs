using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia.Views;

public partial class SettingsDialogWindow : CustomChromeWindow
{
    private SettingsDialogWindowViewModel? _subscribedViewModel;
    private int _navigationVersion;
    private Control? _highlightedSetting;

    public void NavigateToSetting(string? target)
    {
        var version = ++_navigationVersion;
        _highlightedSetting?.Classes.Remove("setting-highlight");
        _highlightedSetting = null;
        if (string.IsNullOrWhiteSpace(target)) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (version != _navigationVersion) return;
            var setting = SettingsContent.GetVisualDescendants()
                .OfType<Control>()
                .FirstOrDefault(control => control.Name == target);
            if (setting is null) return;

            setting.BringIntoView();
            setting.Classes.Add("setting-highlight");
            _highlightedSetting = setting;
            _ = ClearHighlightAsync(setting, version);
        }, DispatcherPriority.Loaded);
    }

    private async Task ClearHighlightAsync(Control setting, int version)
    {
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        if (version != _navigationVersion) return;
        setting.Classes.Remove("setting-highlight");
        _highlightedSetting = null;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UnsubscribeNavigation();
        _subscribedViewModel = DataContext as SettingsDialogWindowViewModel;
        if (_subscribedViewModel is not null)
            _subscribedViewModel.NavigationRequested += NavigateToSetting;
    }

    private void UnsubscribeNavigation()
    {
        if (_subscribedViewModel is not null)
            _subscribedViewModel.NavigationRequested -= NavigateToSetting;
        _subscribedViewModel = null;
    }

    public SettingsDialogWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += (_, _) => UnsubscribeNavigation();
        DataContext = new SettingsDialogWindowViewModel();
        WireCaptionButtons(
            this.FindControl<Button>("MinimizeButton"),
            null,
            this.FindControl<Button>("CloseButton"));
    }
}
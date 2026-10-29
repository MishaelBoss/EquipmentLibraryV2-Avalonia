using Avalonia;
using Avalonia.Controls;

namespace EquipmentLibraryV2_Avalonia.Views.Components;

public partial class TitleBarControlUserControlView : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<TitleBarControlUserControlView, string>(
            nameof(Title),
            defaultValue: string.Empty);

    public static readonly StyledProperty<bool> IsMinimizeButtonProperty =
        AvaloniaProperty.Register<TitleBarControlUserControlView, bool>(
            nameof(IsMinimizeButton),
            defaultValue: true);

    public static readonly StyledProperty<bool> IsMaximizeRestoreButtonProperty =
        AvaloniaProperty.Register<TitleBarControlUserControlView, bool>(
            nameof(IsMaximizeRestoreButton),
            defaultValue: true);

    public static readonly StyledProperty<bool> IsCloseButtonProperty =
        AvaloniaProperty.Register<TitleBarControlUserControlView, bool>(
            nameof(IsCloseButton),
            defaultValue: true);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsMinimizeButton
    {
        get => GetValue(IsMinimizeButtonProperty);
        set => SetValue(IsMinimizeButtonProperty, value);
    }

    public bool IsMaximizeRestoreButton
    {
        get => GetValue(IsMaximizeRestoreButtonProperty);
        set => SetValue(IsMaximizeRestoreButtonProperty, value);
    }

    public bool IsCloseButton
    {
        get => GetValue(IsCloseButtonProperty);
        set => SetValue(IsCloseButtonProperty, value);
    }
    
    public TitleBarControlUserControlView()
    {
        InitializeComponent();
        WireCaptionButtons(
            this.FindControl<Button>("MinimizeButton"),
            this.FindControl<Button>("MaximizeRestoreButton"),
            this.FindControl<Button>("CloseButton"));
    }
    
    private void WireCaptionButtons(
        Button? minimize,
        Button? maximize,
        Button? close)
    {
        if (minimize is not null)
            minimize.Click += (_, _) =>
            {
                if (TopLevel.GetTopLevel(this) is Window window)
                    window.WindowState = WindowState.Minimized;
            };

        if (maximize is not null)
            maximize.Click += (_, _) =>
            {
                if (TopLevel.GetTopLevel(this) is not Window window)
                    return;

                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            };

        if (close is not null)
            close.Click += (_, _) =>
                (TopLevel.GetTopLevel(this) as Window)?.Close();
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace EquipmentLibraryV2_Avalonia.Views;

/// <summary>
/// Base window with a custom title bar. Uses WindowDecorations.Full + ExtendClientAreaToDecorationsHint,
/// so native DWM minimize/maximize/close animations are preserved (see Avalonia issue #21212).
/// Derived XAML must contain buttons named MinimizeButton / MaximizeButton / CloseButton
/// (optional) and handlers are wired up here by name.
/// </summary>
public abstract class CustomChromeWindow : Window
{
    protected void WireCaptionButtons(Button? minimize, Button? maximize, Button? close)
    {
        if (minimize is not null)
            minimize.Click += (_, _) => WindowState = WindowState.Minimized;

        if (maximize is not null)
            maximize.Click += (_, _) => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        if (close is not null)
            close.Click += (_, _) => Close();
    }
}

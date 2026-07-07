using System;
using Avalonia;
using Avalonia.Controls;

namespace EquipmentLibraryV2_Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (OperatingSystem.IsWindows())
            {
                WindowDecorations = WindowDecorations.None;
            }

            PropertyChanged += OnWindowStateChanged;
        }

        private void OnWindowStateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == WindowStateProperty)
            {
                var isFullScreen = WindowState is WindowState.Maximized or WindowState.FullScreen;
                MainBorder.CornerRadius = isFullScreen
                    ? new CornerRadius(0)
                    : new CornerRadius(8);
            }
        }

        private void Minimize_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestore_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

            MaximizeIcon.IsVisible = WindowState != WindowState.Maximized;
            RestoreIcon.IsVisible = WindowState == WindowState.Maximized;
        }

        private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}

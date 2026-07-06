using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

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

using Avalonia;
using Avalonia.Controls;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace EquipmentLibraryV2_Avalonia.Views
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_SNAP_FLOATING_CURSOR = 35;
        private const int DWMWA_DISALLOW_PEEK = 37;

        private readonly bool _isWindows11;

        private readonly string _settingsPath = Path.Combine(AppPaths.UserDataDir, "window_settings.json");

        public MainWindow()
        {
            InitializeComponent();
            LoadWindowSettings();

            _isWindows11 = OperatingSystem.IsWindows() && Environment.OSVersion.Version.Build >= 22000;

            if (OperatingSystem.IsWindows() && !_isWindows11)
            {
                WindowDecorations = WindowDecorations.None;
            }

            if (OperatingSystem.IsWindows())
            {
                ButtonStack.IsVisible = !_isWindows11;
                SpacerGrid.IsVisible = _isWindows11;
            }
            else
            {
                ButtonStack.IsVisible = false;
                SpacerGrid.IsVisible = true;
            }

            if (_isWindows11)
            {
                try
                {
                    var handle = TryGetPlatformHandle()?.Handle;
                    if (handle.HasValue && handle.Value != IntPtr.Zero)
                    {
                        int enableSnap = 1;
                        DwmSetWindowAttribute(handle.Value, DWMWA_SNAP_FLOATING_CURSOR, ref enableSnap, sizeof(int));
                    }
                }
                catch { }
            }

            PropertyChanged += OnWindowStateChanged;

            Closing += (s, e) => SaveWindowSettings();
        }

        private void LoadWindowSettings() {
            try {
                if (!File.Exists(_settingsPath)) {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    return;
                }

                string json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<WindowSettings>(json);

                if (settings == null) return;

                WindowStartupLocation = WindowStartupLocation.Manual;

                Width = settings.Width;
                Height = settings.Height;
                Position = new PixelPoint(settings.X, settings.Y);

                if (Enum.TryParse<WindowState>(settings.WindowState, out var state)) 
                    WindowState = state;
            }
            catch {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void SaveWindowSettings()
        {
            try
            {
                var state = WindowState;
                var settings = new WindowSettings { WindowState = state.ToString() };

                if (state == WindowState.Normal)
                {
                    settings.Width = Width;
                    settings.Height = Height;
                    settings.X = Position.X;
                    settings.Y = Position.Y;
                } else {
                    settings.Width = Bounds.Width;
                    settings.Height = Bounds.Height;
                    settings.X = Position.X;
                    settings.Y = Position.Y;
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
            }
        }

        private void OnWindowStateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == WindowStateProperty)
            {
                var isFullScreen = WindowState is WindowState.Maximized or WindowState.FullScreen;
                MainBorder.CornerRadius = isFullScreen ? new CornerRadius(0) : new CornerRadius(8);

                if (MaximizeIcon is not null && RestoreIcon is not null)
                {
                    MaximizeIcon.IsVisible = !isFullScreen;
                    RestoreIcon.IsVisible = isFullScreen;
                }
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
        }

        private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}

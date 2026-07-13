using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace EquipmentLibraryV2_Avalonia.Views
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DwmwaSnapFloatingCursor = 35;

        private readonly bool _isWindows11;

        private readonly string _settingsPath = Path.Combine(AppPaths.UserDataDir, "window_settings.json");

        public MainWindow()
        {
            Serilog.Log.Information("MainWindow constructor started.");
            
            InitializeComponent();
            LoadWindowSettings();

            _isWindows11 = OperatingSystem.IsWindows() && Environment.OSVersion.Version.Build >= 22000;
            Serilog.Log.Information("Detected OS. IsWindows={IsWindows}, IsWindows11={IsWindows11}, Build={Build}",
                OperatingSystem.IsWindows(),
                _isWindows11,
                Environment.OSVersion.Version.Build);
            
            var isWindows = OperatingSystem.IsWindows();
            var isMac = OperatingSystem.IsMacOS();
            var isLinux = OperatingSystem.IsLinux();

            if (isWindows && !_isWindows11)
            {
                WindowDecorations = WindowDecorations.None;
                Serilog.Log.Debug(
                    "Window decorations disabled for non‑Windows 11. IsWindows11={IsWindows11}",
                    _isWindows11);
            }
            else if (isWindows && _isWindows11)
            {
                Serilog.Log.Debug("Running on Windows 11. Keeping default window decorations.");
            }
            else if (isMac)
            {
                Serilog.Log.Information("Running on macOS. Using platform default window decorations.");
            }
            else if (isLinux)
            {
                Serilog.Log.Information("Running on Linux. Using platform default window decorations.");
            }
            else
            {
                Serilog.Log.Warning("Running on unknown OS platform. IsWindows={IsWindows}, IsMac={IsMac}, IsLinux={IsLinux}",
                    isWindows, isMac, isLinux);
            }

            if (OperatingSystem.IsWindows())
            {
                ButtonStack.IsVisible = !_isWindows11;
                SpacerGrid.IsVisible = _isWindows11;
                Serilog.Log.Debug("ButtonStack.IsVisible={ButtonStackVisible}, SpacerGrid.IsVisible={SpacerVisible}",
                    ButtonStack.IsVisible,
                    SpacerGrid.IsVisible);
            }
            else
            {
                ButtonStack.IsVisible = false;
                SpacerGrid.IsVisible = true;
                Serilog.Log.Debug("Non‑Windows OS: ButtonStack hidden, SpacerGrid visible.");
            }

            if (_isWindows11)
            {
                try
                {
                    var handle = TryGetPlatformHandle()?.Handle;
                    if (handle.HasValue && handle.Value != IntPtr.Zero)
                    {
                        var enableSnap = 1;
                        var hr = DwmSetWindowAttribute(handle.Value, DwmwaSnapFloatingCursor, ref enableSnap, sizeof(int));
                        
                        Serilog.Log.Information(
                            "DwmSetWindowAttribute applied for Windows 11 snap. Handle={Handle}, Result={Result}",
                            handle.Value,
                            hr);
                    }
                    else
                    {
                        Serilog.Log.Warning("Failed to get native window handle for DwmSetWindowAttribute on Windows 11.");
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "DwmSetWindowAttribute failed on Windows 11.");
                }
            }

            PropertyChanged += OnWindowStateChanged;
            KeyDown += OnKeyDown;
            Closing += (s, e) =>
            {
                Serilog.Log.Information("MainWindow closing. Saving window settings.");
                SaveWindowSettings();
            };

            Serilog.Log.Information("MainWindow constructor finished.");
        }

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not ViewModels.MainWindowViewModel vm)
                return;
            
            Serilog.Log.Debug("KeyDown received. Key={Key}", e.Key);

            switch (e.Key)
            {
                case Key.Escape:
                {
                    Serilog.Log.Debug("Escape pressed. TopOverlayContent={HasTopOverlay}",
                        vm.TopOverlayContent is not null);

                    if (vm.TopOverlayContent is not null)
                    {
                        vm.TopOverlayContent = null;
                        Serilog.Log.Information("TopOverlayContent closed by Escape key.");
                        e.Handled = true;
                    }
                    
                    break;
                }
                case Key.F5:
                    Serilog.Log.Information("F5 pressed. Sending RefreshDataMessage and starting refresh indicator animation.");
                    
                    WeakReferenceMessenger.Default.Send(new RefreshDataMessage());
                    e.Handled = true;

                    try
                    {
                        RefreshIndicator.IsVisible = true;
                        RefreshIndicator.Opacity = 1;
                        await Task.Delay(2000);
                        RefreshIndicator.Opacity = 0;
                        RefreshIndicator.IsVisible = false;
                        Serilog.Log.Debug("Refresh indicator animation completed.");
                    }
                    catch (Exception ex)
                    {
                        RefreshIndicator.IsVisible = false;
                        Serilog.Log.Warning(ex, "F5 refresh indicator animation failed");
                    }
                    break;
            }
        }

        private void LoadWindowSettings() 
        {
            Serilog.Log.Debug("Loading window settings from {Path}", _settingsPath);
            
            try {
                if (!File.Exists(_settingsPath)) {
                    Serilog.Log.Information("Window settings file not found. Using centered startup location.");
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    return;
                }

                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<WindowSettings>(json);

                if (settings == null)
                {
                    Serilog.Log.Warning("Window settings deserialization returned null. Using centered startup location.");
                    return;
                }

                WindowStartupLocation = WindowStartupLocation.Manual;

                Width = settings.Width;
                Height = settings.Height;
                Position = new PixelPoint(settings.X, settings.Y);
                
                if (Enum.TryParse<WindowState>(settings.WindowState, out var state))
                {
                    WindowState = state;
                    Serilog.Log.Information(
                        "Window settings applied. Width={Width}, Height={Height}, X={X}, Y={Y}, WindowState={WindowState}",
                        settings.Width,
                        settings.Height,
                        settings.X,
                        settings.Y,
                        state);
                }
                else
                {
                    Serilog.Log.Warning("Failed to parse WindowState from settings: {State}", settings.WindowState);
                }
            }
            catch (Exception ex) {
                Serilog.Log.Warning(ex, "Failed to load window settings. Using centered startup location.");
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void SaveWindowSettings()
        {
            Serilog.Log.Debug("Saving window settings to {Path}", _settingsPath);
            
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

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
                
                Serilog.Log.Information(
                    "Window settings saved. Width={Width}, Height={Height}, X={X}, Y={Y}, WindowState={WindowState}",
                    settings.Width,
                    settings.Height,
                    settings.X,
                    settings.Y,
                    settings.WindowState);
            }
            catch (ExternalException ex)
            {
                Serilog.Log.Error(ex, "Failed to save window settings to {Path}", _settingsPath);
            }
        }

        private void OnWindowStateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != WindowStateProperty) return;
            var isFullScreen = WindowState is WindowState.Maximized or WindowState.FullScreen;
            MainBorder.CornerRadius = isFullScreen ? new CornerRadius(0) : new CornerRadius(8);
            
            Serilog.Log.Debug("WindowState changed to {WindowState}. IsFullScreen={IsFullScreen}",
                WindowState,
                isFullScreen);

            if (MaximizeIcon is null || RestoreIcon is null) return;
            MaximizeIcon.IsVisible = !isFullScreen;
            RestoreIcon.IsVisible = isFullScreen;
        }

        private void Minimize_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Serilog.Log.Information("Minimize button clicked.");
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestore_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var newState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

            Serilog.Log.Information("Maximize/Restore button clicked. OldState={OldState}, NewState={NewState}",
                WindowState,
                newState);

            WindowState = newState;
        }

        private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Serilog.Log.Information("Close button clicked. Closing MainWindow.");
            Close();
        }
    }
}

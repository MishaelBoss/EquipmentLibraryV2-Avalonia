using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentLibraryV2_Avalonia.ViewModels;
using EquipmentLibraryV2_Avalonia.Views;
using Serilog;
using System.IO;
using EquipmentLibraryV2_Avalonia.Infrastructure;

namespace EquipmentLibraryV2_Avalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var now = DateTime.Now;
                var datePart = now.ToString("dd_MM_yyyy");

                var logPath = Path.Combine(
                    AppPaths.UserDataDir,
                    "logs",
                    $"launcher-log-{datePart}.txt");

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:dd.MM.yyyy HH:mm:ss}][{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        logPath,
                        outputTemplate: "[{Timestamp:dd.MM.yyyy HH:mm:ss}][{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                Log.Information("Application starting...");

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
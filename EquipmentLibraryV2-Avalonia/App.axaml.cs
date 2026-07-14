using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.ViewModels;
using EquipmentLibraryV2_Avalonia.Views;
using Serilog;
using Serilog.Debugging;
using System.Diagnostics;

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
            SetupSerilog();
            SetupGlobalExceptionHandlers();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow
                {
                    DataContext = AppServices.Get<MainWindowViewModel>(),
                };
                desktop.MainWindow = mainWindow;
                mainWindow.Show();

                if (mainWindow.DataContext is MainWindowViewModel vm)
                    _ = vm.InitializeAsync();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void SetupSerilog()
        {
            var now = DateTime.Now;
            var datePart = now.ToString("dd_MM_yyyy");

            var logPath = Path.Combine(
                AppPaths.UserDataDir,
                "logs",
                $"launcher-log-{datePart}.txt");
            
            SelfLog.Enable(msg => 
            {
                Debug.WriteLine(msg);
                Console.Error.WriteLine(msg);
            });

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:dd.MM.yyyy HH:mm:ss}][{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    logPath,
                    outputTemplate: "[{Timestamp:dd.MM.yyyy HH:mm:ss}][{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        private void SetupGlobalExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Log.Fatal(ex, "Unhandled AppDomain exception. Terminating={IsTerminating}", args.IsTerminating);
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Log.Fatal(args.Exception, "Unobserved task exception");
                args.SetObserved();
            };

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Exit += (_, _) =>
                {
                    Log.Information("Application exiting");
                    Log.CloseAndFlush();
                };
            }
        }
    }
}
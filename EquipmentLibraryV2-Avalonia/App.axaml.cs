using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentLibraryV2_Avalonia.ViewModels;
using EquipmentLibraryV2_Avalonia.Views;
using Serilog;

namespace EquipmentLibraryV2_Avalonia;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SetupGlobalExceptionHandlers();
        
        var russianCulture = new CultureInfo("ru-RU");
        Thread.CurrentThread.CurrentCulture = russianCulture;
        Thread.CurrentThread.CurrentUICulture = russianCulture;

        CultureInfo.DefaultThreadCurrentCulture = russianCulture;
        CultureInfo.DefaultThreadCurrentUICulture = russianCulture;
            
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
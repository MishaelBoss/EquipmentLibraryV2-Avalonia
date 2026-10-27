using System.Diagnostics;
using Avalonia;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using Serilog;
using Serilog.Debugging;

namespace EquipmentLibraryV2_Avalonia;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        SetupSerilog();
        
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal(
                e.ExceptionObject as Exception,
                "Unhandled AppDomain exception. Terminating={Terminating}",
                e.IsTerminating);

            Log.CloseAndFlush();
        };
        
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
        
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled application exception");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    
    
    private static void SetupSerilog()
    {
        var now = DateTime.Now;
        var datePart = now.ToString("dd_MM_yyyy");
        var logsFolder = Path.Combine(AppPaths.UserDataDir, "logs");
        
        var mainLogPath = Path.Combine(logsFolder, $"main-log-{datePart}.txt");
        var errorsLogPath = Path.Combine(logsFolder, $"errors-log-{datePart}.txt");
            
        SelfLog.Enable(msg => 
        {
            Debug.WriteLine(msg);
            Console.Error.WriteLine(msg);
        });

        const string outputTemplate = "[{Timestamp:dd.MM.yyyy HH:mm:ss}][{Level:u3}] {Message:lj}{NewLine}{Exception}";
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly("@l in ['Warning', 'Error', 'Fatal']")
                .WriteTo.File(errorsLogPath, outputTemplate: outputTemplate, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7))
            .WriteTo.Async(a =>
            {
                a.Console(outputTemplate: outputTemplate);
                a.Logger(lc => lc
                    .Filter.ByExcluding(
                        "SourceContext like 'Avalonia%' and @l = 'Debug'")
                    .WriteTo.File(mainLogPath, outputTemplate: outputTemplate, retainedFileCountLimit: 14));
            })
            .CreateLogger();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
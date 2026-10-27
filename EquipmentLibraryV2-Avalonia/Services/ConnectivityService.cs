using Npgsql;
using Serilog;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;

namespace EquipmentLibraryV2_Avalonia.Services;

internal static class ConnectivityService
{
    private static readonly Lock SyncRoot = new();
    private static bool _lastReachable;
    private static long _lastCheckedUtcTicks;

    public static void RememberResult(bool connected)
    {
        lock (SyncRoot)
        {
            _lastReachable = connected;
            _lastCheckedUtcTicks = DateTime.UtcNow.Ticks;
        }
    }

    public static bool IsReachableRecently(TimeSpan within)
    {
        lock (SyncRoot)
        {
            return _lastReachable && DateTime.UtcNow.Ticks - _lastCheckedUtcTicks <= within.Ticks;
        }
    }

    public static async Task<bool> IsServerReachableAsync()
    {
        if (IsReachableRecently(TimeSpan.FromSeconds(60)))
            return true;

        if (!int.TryParse(AppConfig.Port, out var port))
            port = 5432;

        var ok = await IsTcpReachableAsync(AppConfig.Ip, port, TimeSpan.FromSeconds(3));
        RememberResult(ok);
        return ok;
    }

    private static bool IsConfigInvalid() =>
        string.IsNullOrWhiteSpace(AppConfig.Ip) ||
        string.IsNullOrWhiteSpace(AppConfig.Port) ||
        string.IsNullOrWhiteSpace(AppConfig.Database) ||
        string.IsNullOrWhiteSpace(AppConfig.User) ||
        string.IsNullOrWhiteSpace(AppConfig.Password);

    public static async Task<bool> ConnectivityChecker(bool showNotification = true)
    {
        try
        {
            if (IsConfigInvalid())
            {
                Log.Error("Database connection data is incomplete {PropertyValue0}", AppConfig.Database);
                return false;
            }

            if (!int.TryParse(AppConfig.Port, out var port)) port = 5432;
            if (await IsTcpReachableAsync(AppConfig.Ip, port, TimeSpan.FromSeconds(3)))
                return await TestPostgreSqlConnection(showNotification);
            if (showNotification)
                WeakReferenceMessenger.Default.Send(new ShowOrHideNotification(ErrorAction.Add, ErrorUserControlViewModel.Instance, ("PostgreSQL server is not running. Start PostgreSQL to create the database", 503L)));
            return false;

        }
        catch (Exception ex)
        {
            Log.Warning("Connection check failed: {ExMessage}", ex.Message);
            return false;
        }
    }

    public static async Task<bool> IsTcpReachableAsync(string host, int port, TimeSpan timeout)
    {
        using var tcp = new TcpClient();
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await tcp.ConnectAsync(host, port, cts.Token);
            Log.Information("TCP connect to ({Host},{Port}) succeeded", host, port);
            return tcp.Connected;
        }
        catch (OperationCanceledException)
        {
            Log.Warning("TCP connect to ({Host},{Port}) timed out after {TimeoutTotalSeconds:0}s", host, port, timeout.TotalSeconds);
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning("TCP connect to ({Host},{Port}) failed: {ExMessage}", host, port, ex.Message);
            return false;
        }
    }

    private static async Task<bool> TestPostgreSqlConnection(bool showNotification = true)
    {
        var connString = $"Server={AppConfig.Ip};Port={AppConfig.Port};Database={AppConfig.Database};" +
                                  $"User Id={AppConfig.User};Password={AppConfig.Password};" +
                                  $"Timeout=10;CommandTimeout=10;Pooling=true;MaxPoolSize=5;SslMode=Prefer";
        
        try
        {
            await using var connection = new NpgsqlConnection(connString);
            await connection.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT 1", connection);

            Log.Information("PostgreSQL connection test successful, result {Result}", await cmd.ExecuteScalarAsync());
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "PostgreSQL connection failed"); 
            if (showNotification)
                WeakReferenceMessenger.Default.Send(new ShowOrHideNotification(ErrorAction.Add, ErrorUserControlViewModel.Instance, ("PostgreSQL connection failed", 504L)));
            return false;
        }
    }
}

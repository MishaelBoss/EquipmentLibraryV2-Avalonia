using Npgsql;
using Serilog;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;

namespace EquipmentLibraryV2_Avalonia.Services;

internal static class ConnectivityService
{
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

            if (!int.TryParse(AppConfig.Port, out var port))
                port = 5432;

            if (!await IsTcpReachableAsync(AppConfig.Ip, port, TimeSpan.FromSeconds(3)))
            {
                if (showNotification)
                    WeakReferenceMessenger.Default.Send(new ShowOrHideNotification(ErrorAction.Add, ErrorUserControlViewModel.Instance, ("Connection to the server was lost", 503L)));
                return false;
            }

            return await TestPostgreSqlConnection(showNotification);
        }
        catch (Exception ex)
        {
            Log.Warning($"Connection check failed: {ex.Message}");
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
            Log.Information($"TCP connect to ({host},{port}) succeeded");
            return tcp.Connected;
        }
        catch (OperationCanceledException)
        {
            Log.Warning($"TCP connect to ({host},{port}) timed out after {timeout.TotalSeconds:0}s");
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning($"TCP connect to ({host},{port}) failed: {ex.Message}");
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
            var result = await cmd.ExecuteScalarAsync();

            Log.Information($"PostgreSQL connection test successful, result {result}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning($"PostgreSQL connection failed: {ex.Message}");
            if (showNotification)
                WeakReferenceMessenger.Default.Send(new ShowOrHideNotification(ErrorAction.Add, ErrorUserControlViewModel.Instance, ("PostgreSQL connection failed", 504L)));
            return false;
        }
    }
}

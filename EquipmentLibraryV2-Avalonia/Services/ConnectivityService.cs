using Npgsql;
using Serilog;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;

namespace EquipmentLibraryV2_Avalonia.Services;

internal static class ConnectivityService
{
    public static async Task<bool> ConnectivityChecker()
    {
        try
        {
            var ip = AppConfig.Ip;
            if (string.IsNullOrWhiteSpace(ip))
            {
                Log.Error("Database IP is not configured");
                return false;
            }

            if (!await PingHostAsync(ip))
                return false;

            return await TestPostgreSqlConnection();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Connectivity check failed");
            return false;
        }
    }

    private static async Task<bool> PingHostAsync(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 3000);

            if (reply is not { Status: IPStatus.Success })
            {
                WeakReferenceMessenger.Default.Send(
                    new ShowOrHideNotification(ErrorAction.Add, ErrorUserControlViewModel.Instance,
                        ("Сервер недоступен", 503L)));
                return false;
            }

            return true;
        }
        catch (PingException ex)
        {
            Log.Warning("Ping failed for {Host}: {Message}", host, ex.Message);
            return false;
        }
    }

    private static async Task<bool> TestPostgreSqlConnection()
    {
        var connString = $"Server={AppConfig.Ip};Port={AppConfig.Port};Database={AppConfig.Database};" +
                         $"User Id={AppConfig.User};Password={AppConfig.Password};" +
                         $"Timeout=10;CommandTimeout=10;Pooling=true;MaxPoolSize=5";

        try
        {
            await using var connection = new NpgsqlConnection(connString);
            await connection.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT 1", connection);
            var result = await cmd.ExecuteScalarAsync();

            Log.Information("PostgreSQL connection test OK, result={Result}", result);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning("PostgreSQL connection failed: {Message}", ex.Message);
            WeakReferenceMessenger.Default.Send(
                new ShowOrHideNotification(ErrorAction.Add, ErrorUserControlViewModel.Instance,
                    ("База данных недоступна", 504L)));
            return false;
        }
    }

    public static NpgsqlConnection CreateConnection()
    {
        var conn = new NpgsqlConnection($"Server={AppConfig.Ip};Port={AppConfig.Port};Database={AppConfig.Database};" +
                                        $"User Id={AppConfig.User};Password={AppConfig.Password};" +
                                        $"SslMode=Disable;Pooling=true;MaxPoolSize=20;Timeout=10;CommandTimeout=10");
        return conn;
    }
}

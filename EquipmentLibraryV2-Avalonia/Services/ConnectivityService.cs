using Npgsql;
using Serilog;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
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

    public static async Task<bool> ConnectivityChecker()
    {
        try
        {
            if (IsConfigInvalid())
            {
                Log.Error("Database connection data is incomplete {PropertyValue0}", AppConfig.Database);
                return false;
            }

            using var ping = new Ping();
            const string? hostName = AppConfig.Ip;
            var reply = ping.Send(hostName, 3000);

            Log.Information($"Ping status for ({hostName}): {reply.Status}");

            if (reply is not { Status: IPStatus.Success })
            {
                WeakReferenceMessenger.Default.Send(new ShowOrHideError(ErrorAction.Add, new ConnectionErrorUserControlViewModel()));
                return false;
            }
            
            Log.Information($"Address: {reply.Address}");
            Log.Information($"Roundtrip time: {reply.RoundtripTime}");
            Log.Information($"Time to live: {reply.Options?.Ttl}");
            return await TestPostgreSqlConnection();

        }
        catch (PingException ex)
        {
            Log.Warning($"Ping failed: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning($"Ping failed: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> TestPostgreSqlConnection()
    {
        const string connString = $"Server={AppConfig.Ip};" +
                                  $"Port={AppConfig.Port};" +
                                  $"Database={AppConfig.Database};" +
                                  $"User Id={AppConfig.User};" +
                                  $"Password={AppConfig.Password};" +
                                  $"Timeout=5;" +
                                  $"CommandTimeout=5";

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
            WeakReferenceMessenger.Default.Send(new ShowOrHideError(ErrorAction.Add, new ConnectionErrorUserControlViewModel()));
            return false;
        }
    }
}

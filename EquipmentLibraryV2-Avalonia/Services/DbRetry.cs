using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Serilog;

namespace EquipmentLibraryV2_Avalonia.Services;

public static class DbRetry
{
    private static readonly int[] RetryDelaysMs = [200, 500, 1000, 2000];

    public static async Task<T> ExecuteAsync<T>(Func<NpgsqlConnection, Task<T>> operation, string context = "", CancellationToken ct = default)
    {
        var lastEx = default(Exception);

        for (var attempt = 0; attempt <= RetryDelaysMs.Length; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await using var connection = ConnectivityService.CreateConnection();
                await connection.OpenAsync(ct);
                return await operation(connection);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NpgsqlException ex) when (attempt < RetryDelaysMs.Length && IsTransient(ex))
            {
                lastEx = ex;
                Log.Warning(ex, "DB transient error (attempt {Attempt}/{Max}) in {Context}. Retrying in {Delay}ms",
                    attempt + 1, RetryDelaysMs.Length, context, RetryDelaysMs[attempt]);
                await Task.Delay(RetryDelaysMs[attempt], ct);
            }
            catch (Exception ex)
            {
                lastEx = ex;
                Log.Error(ex, "DB non-transient error in {Context}", context);
                throw;
            }
        }

        throw lastEx ?? new InvalidOperationException("DB retry exhausted with no result");
    }

    public static async Task ExecuteAsync(Func<NpgsqlConnection, Task> operation, string context = "", CancellationToken ct = default)
    {
        await ExecuteAsync<object?>(async conn =>
        {
            await operation(conn);
            return null;
        }, context, ct);
    }

    private static bool IsTransient(NpgsqlException ex)
    {
        return ex.InnerException is System.Net.Sockets.SocketException
            || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || (ex.SqlState is not null && (
                ex.SqlState.StartsWith("08", StringComparison.Ordinal)  // connection errors
                || ex.SqlState == "40001"                               // serialization failure
                || ex.SqlState == "40P01"));                            // deadlock
    }
}

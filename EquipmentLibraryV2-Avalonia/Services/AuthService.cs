using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.Modelsl;
using Npgsql;
using Serilog;
using System.Security.Cryptography;
using System.Text.Json;

namespace EquipmentLibraryV2_Avalonia.Services;

public static class AuthService
{
    public static UserSession? CurrentSession { get; private set; }

    private static CancellationTokenSource? _healthCheckCts;
    private static Task? _healthCheckTask;

    private static string CookiePath => Path.Combine(AppPaths.UserDataDir, "login.cookie");

    public static async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            if (!await ConnectivityService.ConnectivityChecker())
            {
                Log.Warning("LoginAsync aborted for user {Username}: no connectivity", username);
                return false;
            }

            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync();

            const string sql = "SELECT id AS Id, login AS Login, user_type_id AS UserRole FROM public.users WHERE login = @u AND password = crypt(@p, password) AND is_active = true";
            
            var user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { u = username, p = password });

            if (user == null)
            {
                Log.Warning("Login failed for user {Username}: user not found or invalid password", username);
                return false;
            }

            CurrentSession = new UserSession(
                Id: user.Id,
                Login: user.Login,
                UserRole: user.UserRole
            );
            
            Log.Information("User {Username} logged in successfully. Session id: {UserId}, role: {UserRole}",
                user.Login, user.Id, user.UserRole);

            var refreshToken = GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            await SaveRefreshTokenAsync(user.Id, refreshToken, expiresAt);
            await SaveTokenToFileAsync(refreshToken, expiresAt);

            WeakReferenceMessenger.Default.Send(new LoginMessage());

            StartSessionHealthCheck();
            
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "LoginAsync failed for user {Username}", username);
            return false;
        }
    }

    private static async Task SaveTokenToFileAsync(string refreshToken, DateTime expires)
    {
        try
        {
            if (!Directory.Exists(AppPaths.UserDataDir)) Directory.CreateDirectory(AppPaths.UserDataDir);

            var data = new
            {
                RefreshToken = refreshToken,
                Expires = expires
            };

            await File.WriteAllTextAsync(CookiePath, JsonSerializer.Serialize(data));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save login cookie");
            throw;
        }
    }

    public static async Task<bool> TryAutoLoginAsync()
    {
        Log.Information("TryAutoLoginAsync started");
        
        try
        {
            if (!File.Exists(CookiePath))
            {
                Log.Warning("Auto-login cookie not found at {CookiePath}", CookiePath);
                return false;
            }

            var json = await File.ReadAllTextAsync(CookiePath);
            var data = JsonSerializer.Deserialize<RefreshTokenData>(json);

            if (data == null)
            {
                Log.Warning("Auto-login cookie deserialization returned null");
                return false;
            }

            if (data.Expires <= DateTime.UtcNow)
            {
                return false;
            }

            if (!await ConnectivityService.ConnectivityChecker())
            {
                Log.Warning("Auto-login aborted: no connectivity");
                return false;
            }

            var result = await VerifySessionInDbAsync(data);
            Log.Information("TryAutoLoginAsync finished with result {Result}", result);

            return result;
        }
        catch (Exception ex)
        {
            Log.Logger.Error($"Auto-login failed {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> VerifySessionInDbAsync(RefreshTokenData data)
    {
        try
        {
            const string sql = @"
                            SELECT u.id, u.login, u.user_type_id
                            FROM public.user_refresh_tokens rt
                            JOIN public.users u on u.id = rt.user_id
                            WHERE rt.token = @Token
                              AND rt.expires_at > now()
                              AND rt.revoked_at is null
                              AND u.is_active = true
                            LIMIT 1";

            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync();

            var row = await connection.QueryFirstOrDefaultAsync(sql, new { Token = data.RefreshToken });

            if (row == null)
            {
                Log.Warning("Auto-login verification failed: token not found or expired");
                return false;
            }

            CurrentSession = new UserSession(
                Id: (long)row.id,
                Login: (string)row.login,
                UserRole: (long)row.user_type_id
            );

            Log.Information("Auto-login successful for user {Login}, session id {UserId}, role {UserRole}",
            CurrentSession.Login, CurrentSession.Id, CurrentSession.UserRole);

            StartSessionHealthCheck();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "VerifySessionInDbAsync failed");
            return false;
        }
    }

    public static void Logout()
    {
        Log.Information("Logout started");

        StopSessionHealthCheck();

        try
        {
            if (File.Exists(CookiePath))
            {
                File.Delete(CookiePath);
                Log.Debug("Login cookie deleted at {CookiePath}", CookiePath);
            }
            CurrentSession = null;
            WeakReferenceMessenger.Default.Send(new LogoutMessage());
            
            Log.Information("Logout completed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Logout failed");
        }
    }

    public static void StartSessionHealthCheck()
    {
        StopSessionHealthCheck();
        _healthCheckCts = new CancellationTokenSource();
        _healthCheckTask = SessionHealthCheckLoopAsync(_healthCheckCts.Token);
    }

    public static void StopSessionHealthCheck()
    {
        _healthCheckCts?.Cancel();
        _healthCheckCts?.Dispose();
        _healthCheckCts = null;
    }

    private static async Task SessionHealthCheckLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                if (CurrentSession is null)
                    continue;

                await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
                const string sql = "SELECT is_active FROM public.users WHERE id = @id";
                var isActive = await connection.ExecuteScalarAsync<bool?>(sql, new { id = CurrentSession.Id });

                if (isActive == false)
                {
                    Log.Warning("Session {UserId} deactivated by admin. Forcing logout.", CurrentSession.Id);
                    Logout();
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Session health check error");
            }
        }
    }

    public static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static async Task SaveRefreshTokenAsync(long userId, string token, DateTime expiresAt)
    {
        const string sql = "INSERT INTO public.user_refresh_tokens (user_id, token, expires_at) VALUES (@UserId, @Token, @ExpiresAt)";

        await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
        await connection.OpenAsync();

        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt
        });
    }
}

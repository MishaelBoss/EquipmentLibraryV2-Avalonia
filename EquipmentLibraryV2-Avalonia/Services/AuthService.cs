using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using EquipmentLibraryV2_Avalonia.Models;
using Npgsql;
using Serilog;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;

namespace EquipmentLibraryV2_Avalonia.Services;

public static class AuthService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

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

            var token = GenerateSessionToken();

            await SaveSessionAsync(connection, user.Id, token);

            Log.Information("User {Username} logged in successfully. Session id: {UserId}, role: {UserRole}",
                user.Login, user.Id, user.UserRole);

            await SaveLoginCookieAsync(CurrentSession.Id, CurrentSession.Login, token, DateTime.Now.Add(SessionLifetime));

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

    private static string GenerateSessionToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static async Task SaveSessionAsync(NpgsqlConnection connection, int userId, string token)
    {
        await using var cleanup = new NpgsqlCommand("DELETE FROM public.user_sessions WHERE expires_at < now()", connection);
        await cleanup.ExecuteNonQueryAsync();

        await connection.ExecuteAsync(
            "INSERT INTO public.user_sessions (user_id, token_hash, expires_at) VALUES (@userId, @tokenHash, @expiresAt)",
            new
            {
                userId,
                tokenHash = HashToken(token),
                expiresAt = DateTime.UtcNow.Add(SessionLifetime)
            });
    }

    private static async Task DeleteSessionByTokenAsync(string token)
    {
        try
        {
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync();

            await connection.ExecuteAsync(
                "DELETE FROM public.user_sessions WHERE token_hash = @tokenHash",
                new { tokenHash = HashToken(token) });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to revoke session token in database");
        }
    }

    private static void DeleteLoginCookie()
    {
        try
        {
            if (!File.Exists(CookiePath)) return;
            
            File.Delete(CookiePath);
            Log.Debug("Login cookie deleted at {CookiePath}", CookiePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete login cookie at {CookiePath}", CookiePath);
        }
    }

    private static async Task SaveLoginCookieAsync(double id, string login, string token, DateTime expires)
    {
        Log.Debug("Saving login cookie for user {Login}. Expires at {Expires}", login, expires);

        try
        {
            if (!Directory.Exists(AppPaths.UserDataDir)) Directory.CreateDirectory(AppPaths.UserDataDir);

            var data = new
            {
                Id = id,
                Login = login,
                Token = token,
                Expires = expires
            };

            await File.WriteAllTextAsync(CookiePath, JsonSerializer.Serialize(data));
            Log.Information("Login cookie saved for user {Login}", login);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save login cookie for user {Login}", login);
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
            var data = JsonSerializer.Deserialize<CookieData>(json);

            if (data == null || string.IsNullOrWhiteSpace(data.Token))
            {
                Log.Warning("Auto-login cookie is invalid or has no session token");
                DeleteLoginCookie();
                return false;
            }

            if (data.Expires <= DateTime.Now)
            {
                Log.Warning("Auto-login cookie expired for user {Login}. Expired at {Expires}", data.Login, data.Expires);
                DeleteLoginCookie();
                _ = DeleteSessionByTokenAsync(data.Token);
                return false;
            }

            if (!await ConnectivityService.ConnectivityChecker())
            {
                Log.Warning("Auto-login aborted: no connectivity");
                return false;
            }

            var result = await VerifySessionInDbAsync(data);

            if (!result)
            {
                DeleteLoginCookie();
                await DeleteSessionByTokenAsync(data.Token);
            }

            Log.Information("TryAutoLoginAsync finished with result {Result}", result);

            return result;
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Auto-login failed {ExMessage}", ex.Message);
            return false;
        }
    }

    private static async Task<bool> VerifySessionInDbAsync(CookieData data)
    {
        Log.Debug("Verifying auto-login session in database for user {Login}", data.Login);
        
        try
        {
            const string sql = """
                SELECT u.id AS Id, u.login AS Login, u.user_type_id AS UserRole
                FROM public.users u
                INNER JOIN public.user_sessions s ON s.user_id = u.id
                WHERE s.token_hash = @tokenHash
                  AND s.expires_at > now()
                  AND u.is_active = true
                """;

            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync();

            var user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { tokenHash = HashToken(data.Token) });

            if (user == null)
            {
                Log.Warning("Auto-login verification failed: no active session found for the stored token");
                return false;
            }

            if (!string.Equals(user.Login, data.Login, StringComparison.Ordinal))
            {
                Log.Warning("Auto-login verification failed: cookie login {CookieLogin} does not match DB login {DbLogin}",
                    data.Login, user.Login);
                return false;
            }

            CurrentSession = new UserSession(
                Id: user.Id,
                Login: user.Login,
                UserRole: user.UserRole
            );
            
            Log.Information("Auto-login successful for user {Login}, session id {UserId}, role {UserRole}",
                user.Login, CurrentSession.Id, CurrentSession.UserRole);

            StartSessionHealthCheck();
            
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "VerifySessionInDbAsync failed for user {Login}", data.Login);
            throw;
        }
    }

    public static void Logout()
    {
        Log.Information("Logout started");

        StopSessionHealthCheck();

        try
        {
            var token = ReadCookieToken();

            if (token is not null)
            {
                _ = DeleteSessionByTokenAsync(token);
            }

            DeleteLoginCookie();

            CurrentSession = null;
            WeakReferenceMessenger.Default.Send(new LogoutMessage());
            
            Log.Information("Logout completed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Logout failed");
        }
    }

    private static string? ReadCookieToken()
    {
        try
        {
            if (!File.Exists(CookiePath)) return null;

            var json = File.ReadAllText(CookiePath);
            return JsonSerializer.Deserialize<CookieData>(json)?.Token;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read login cookie for session revocation");
            return null;
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
                var session = CurrentSession;
                if (session is null)
                    continue;

                await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
                await connection.OpenAsync();

                const string sql = "SELECT is_active FROM public.users WHERE id = @id";
                var isActive = await connection.ExecuteScalarAsync<bool?>(sql, new { id = session.Id });

                if (isActive == true) continue;
                
                Log.Warning("Session {UserId} is no longer valid (user missing or deactivated). Forcing logout", session.Id);
                Logout();
                break;
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
}

using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using EquipmentLibraryV2_Avalonia.Models;
using Npgsql;
using Serilog;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;

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

            await SaveLoginCookieAsync(CurrentSession.Id, CurrentSession.Login, Guid.NewGuid().ToString(), DateTime.Now.AddDays(7));

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
            var data = JsonSerializer.Deserialize<CookieData>(json);

            if (data == null)
            {
                Log.Warning("Auto-login cookie deserialization returned null");
                return false;
            }

            if (data.Expires <= DateTime.Now)
            {
                Log.Warning("Auto-login cookie expired for user {Login}. Expired at {Expires}", data.Login, data.Expires);
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

    private static async Task<bool> VerifySessionInDbAsync(CookieData data)
    {
        Log.Debug("Verifying auto-login session in database for user {Login}", data.Login);
        
        try
        {
            const string sql = "SELECT id, login, user_type_id FROM public.users WHERE id = @id AND is_active = true";

            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", data.Id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                Log.Warning("Auto-login verification failed: user id {UserId} not found in DB", data.Id);
                return false;
            }
            var loginInDb = reader.GetString(1);

            if (loginInDb != data.Login)
            {
                Log.Warning("Auto-login verification failed: cookie login {CookieLogin} does not match DB login {DbLogin}",
                    data.Login, loginInDb);
                return false;
            }

            CurrentSession = new UserSession(
                Id: reader.GetInt64(0),
                Login: loginInDb,
                UserRole: reader.GetInt64(2)
            );
            
            Log.Information("Auto-login successful for user {Login}, session id {UserId}, role {UserRole}",
                loginInDb, CurrentSession.Id, CurrentSession.UserRole);

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
}

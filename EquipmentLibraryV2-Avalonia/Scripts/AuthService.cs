using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using EquipmentLibraryV2_Avalonia.Models;
using Npgsql;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EquipmentLibraryV2_Avalonia.Scripts;

public static class AuthService
{
    public static UserSession? CurrentSession { get; private set; }

    private static string CookiePath => Path.Combine(AppPaths.UserDataDir, "login.cookie");

    public static async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            if (!await ConnectivityService.ConnectivityChecker()) return false;

            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync();

            const string sql = "SELECT id, login, user_type_id FROM public.users WHERE login = @u AND password = @p AND is_active = true";

            var user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { u = username, p = password });

            if (user == null) return false;

            CurrentSession = new UserSession(
                Id: user.Id,
                Login: user.Login,
                UserRole: user.UserRole
            );

            await SaveLoginCookieAsync(CurrentSession.Id, CurrentSession.Login, Guid.NewGuid().ToString(), DateTime.Now.AddDays(7));

            WeakReferenceMessenger.Default.Send(new LoginMessage());

            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex.Message);
            return false;
        }
    }

    private static async Task SaveLoginCookieAsync(double id, string login, string token, DateTime expires)
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
    }

    public static async Task<bool> TryAutoLoginAsync()
    {
        try
        {
            if (!File.Exists(CookiePath)) return false;

            var json = await File.ReadAllTextAsync(CookiePath);
            var data = JsonSerializer.Deserialize<CookieData>(json);

            if (data == null || data.Expires <= DateTime.Now) return false;

            if (!await ConnectivityService.ConnectivityChecker()) return false;

            return await VerifySessionInDbAsync(data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error($"Auto-login failed {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> VerifySessionInDbAsync(CookieData data)
    {
        const string sql = "SELECT id, login, user_type_id FROM public.users WHERE id = @id AND is_active = true";

        await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", data.Id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return false;
        var loginInDb = reader.GetString(1);

        if (loginInDb != data.Login) return false;

        CurrentSession = new UserSession(
            Id: reader.GetInt64(0),
            Login: loginInDb,
            UserRole: reader.GetInt64(2)
        );
        return true;

    }

    public static void Logout()
    {
        if (File.Exists(CookiePath)) File.Delete(CookiePath);
        CurrentSession = null;
        WeakReferenceMessenger.Default.Send(new LogoutMessage());
    }
}

using System.Reflection;
using DotNetEnv;
using Npgsql;
using Serilog;

namespace EquipmentLibraryV2_Avalonia.Infrastructure;

public abstract class AppConfig
{
    static AppConfig()
    {
        if (!App.IsDeveloperMode) return;
        
        try
        {
            Env.NoClobber().TraversePath().Load();
        }
        catch
        {
            Log.Error("Could not load environment variables from .env");
        }
    }

    private static readonly Assembly ApplicationAssembly = Assembly.GetEntryAssembly() ?? typeof(App).Assembly;
    public static string Version =>
        ApplicationAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?.Split('+')[0]
        ?? ApplicationAssembly.GetName().Version?.ToString(3)
        ?? "unknown";

    public static string DisplayVersion => $"EquipmentLibrary v2: {Version}";

    #region DB

    private static string? GetEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string DefaultIp => GetEnv("DB_HOST") ?? "localhost";
    public static string DefaultPort => GetEnv("DB_PORT") ?? "5432";
    public static string DefaultDatabase => GetEnv("DB_NAME") ?? "ELA_V2";
    public static string DefaultUser => GetEnv("DB_USER") ?? string.Empty;
    public static string DefaultPassword => GetEnv("DB_PASSWORD") ?? string.Empty;

    public static string Ip => LoadSetting(s => s.Ip, DefaultIp);
    public static string Port => LoadSetting(s => s.Port, DefaultPort);
    public static string Database => LoadSetting(s => s.Database, DefaultDatabase);
    public static string User => LoadSetting(s => s.User, DefaultUser);
    public static string Password => LoadSetting(s => s.Password, DefaultPassword);

    public static bool HasCredentials =>
        !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password);

    private static string LoadSetting(Func<AppSettings, string> selector, string fallback)
    {
        try
        {
            var value = selector(AppSettings.Load());
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
        catch
        {
            return fallback;
        }
    }

    private static string FirstNonEmpty(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? _connectionString;
    private static readonly Lock ConnectionLock = new();

    public static Task<string> ConnectionAsync() =>
        Task.FromResult(GetConnectionString());

    public static string GetConnectionString()
    {
        lock (ConnectionLock)
        {
            if (!string.IsNullOrEmpty(_connectionString))
                return _connectionString;

            var settings = AppSettings.Load();

            var host = FirstNonEmpty(settings.Ip, DefaultIp);
            var port = FirstNonEmpty(settings.Port, DefaultPort);
            var database = FirstNonEmpty(settings.Database, DefaultDatabase);
            var user = FirstNonEmpty(settings.User, DefaultUser);
            var password = FirstNonEmpty(settings.Password, DefaultPassword);

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Database credentials are not configured. Set DB_USER and DB_PASSWORD " +
                    "(DEBUG: .env in the repository root; RELEASE: OS environment variables) " +
                    "or configure the connection in Settings.");
            }

            var connectionString = BuildConnectionString(host, port, database, user, password);
            _connectionString = connectionString;
            return connectionString;
        }
    }

    public static string BuildConnectionString(
        string host,
        string port,
        string database,
        string user,
        string password,
        int timeoutSeconds = 10,
        int commandTimeoutSeconds = 30,
        int maxPoolSize = 5)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Database host is not configured.");

        if (string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("Database name is not configured.");

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Database credentials are not configured.");

        if (!int.TryParse(port, out var portNumber) || portNumber is < 1 or > 65535)
            throw new InvalidOperationException($"Database port '{port}' is invalid.");

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host.Trim(),
            Port = portNumber,
            Database = database.Trim(),
            Username = user,
            Password = password,
            SslMode = GetSslMode(),
            Timeout = timeoutSeconds,
            CommandTimeout = commandTimeoutSeconds,
            Pooling = true,
            MaxPoolSize = maxPoolSize
        };

        return builder.ConnectionString;
    }

    private static SslMode GetSslMode()
    {
        var raw = GetEnv("DB_SSL_MODE");
        return raw is not null && Enum.TryParse<SslMode>(raw, ignoreCase: true, out var mode)
            ? mode
            : SslMode.Prefer;
    }

    public static void ResetConnection()
    {
        lock (ConnectionLock)
        {
            _connectionString = null;
        }
    }
    #endregion
}

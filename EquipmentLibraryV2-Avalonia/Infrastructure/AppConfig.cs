using System.Reflection;
using Serilog;

namespace EquipmentLibraryV2_Avalonia.Infrastructure
{
    public abstract class AppConfig
    {
        private static readonly Assembly ApplicationAssembly = Assembly.GetEntryAssembly() ?? typeof(App).Assembly;

        public static string Version =>
            ApplicationAssembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                ?.Split('+')[0]
            ?? ApplicationAssembly.GetName().Version?.ToString(3)
            ?? "unknown";

        public static string DisplayVersion => $"EquipmentLibrary v2: {Version}";

        private static DbCredentials? _cachedCredentials;
        private static readonly object _credLock = new();

        public static string Ip => GetCredentials().Ip;
        public static string Port => GetCredentials().Port;
        public static string Database => GetCredentials().Database;
        public static string User => GetCredentials().User;
        public static string Password => GetCredentials().Password;

        private static DbCredentials GetCredentials()
        {
            if (_cachedCredentials is not null)
                return _cachedCredentials;

            lock (_credLock)
            {
                _cachedCredentials ??= DbCredentials.Load();
            }

            return _cachedCredentials;
        }

        public static void ReloadCredentials()
        {
            lock (_credLock)
            {
                _cachedCredentials = DbCredentials.Load();
                _connectionString = null;
            }
        }

        private static string? _connectionString;

        public static string ConnectionString()
        {
            if (!string.IsNullOrEmpty(_connectionString))
                return _connectionString;

            var conn = $"Server={Ip};Port={Port};Database={Database};User Id={User};Password={Password};SslMode=Disable;Pooling=true;MaxPoolSize=20;Timeout=10;CommandTimeout=10";

            _connectionString = conn;
            return _connectionString;
        }

        public static void ResetConnection()
        {
            _connectionString = null;
        }
    }
}

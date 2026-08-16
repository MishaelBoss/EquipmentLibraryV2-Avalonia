using EquipmentLibraryV2_Avalonia.Services;
using System.Reflection;

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

        #region DB

        public const string DefaultIp = "localhost";
        public const string DefaultPort = "5432";
        public const string DefaultDatabase = "ELA_V2";
        public const string DefaultUser = "postgres";
        public const string DefaultPassword = "cr2032";

        public static string Ip => LoadSetting(s => s.Ip, DefaultIp);
        public static string Port => LoadSetting(s => s.Port, DefaultPort);
        public static string Database => LoadSetting(s => s.Database, DefaultDatabase);
        public static string User => LoadSetting(s => s.User, DefaultUser);
        public static string Password => LoadSetting(s => s.Password, DefaultPassword);

        private static string LoadSetting(Func<AppSettings, string> selector, string fallback)
        {
            try
            {
                var value = selector(AppSettings.Load());
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            }
            catch
            {
                return fallback;
            }
        }

        private static string Pick(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;

        private static string? _connectionString;

        public static async Task<string> ConnectionAsync()
        {
            if (!string.IsNullOrEmpty(_connectionString))
                return _connectionString;

            var settings = AppSettings.Load();

            var ip = Pick(settings.Ip, DefaultIp);
            var port = Pick(settings.Port, DefaultPort);
            var database = Pick(settings.Database, DefaultDatabase);
            var user = Pick(settings.User, DefaultUser);
            var password = Pick(settings.Password, DefaultPassword);

            var hasConnectionData = !string.IsNullOrWhiteSpace(ip) &&
                                    !string.IsNullOrWhiteSpace(port) &&
                                    !string.IsNullOrWhiteSpace(database) &&
                                    !string.IsNullOrWhiteSpace(user) &&
                                    !string.IsNullOrWhiteSpace(password);

            var baseConnection = $"Server={ip};Port={port};Database={database};User Id={user};Password={password};SslMode=Disable";

            if (!hasConnectionData)
            {
                _connectionString = await ConnectivityService.ConnectivityChecker() ? baseConnection : string.Empty;
            }
            else
            {
                _connectionString = baseConnection;
            }

            return _connectionString;
        }

        public static void ResetConnection()
        {
            _connectionString = null;
        }
        #endregion
    }
}
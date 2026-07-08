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

        public const string? Ip = "localhost";
        public const string? Port = "5432";
        public const string? Database = "ELA_V2";
        public const string? User = "postgres";
        public const string? Password = "cr2032";

        private static string? _connectionString;

        public static async Task<string> ConnectionAsync()
        {
            if (!string.IsNullOrEmpty(_connectionString))
                return _connectionString;

            
            var hasConnectionData = !string.IsNullOrWhiteSpace(Ip) &&
                                    !string.IsNullOrWhiteSpace(Port) &&
                                    !string.IsNullOrWhiteSpace(Database) &&
                                    !string.IsNullOrWhiteSpace(User) &&
                                    !string.IsNullOrWhiteSpace(Password);

            var baseConnection = $"Server={Ip};Port={Port};Database={Database};User Id={User};Password={Password};SslMode=Disable";
            
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
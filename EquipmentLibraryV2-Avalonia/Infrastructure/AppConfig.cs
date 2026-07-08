using EquipmentLibraryV2_Avalonia.Services;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace EquipmentLibraryV2_Avalonia.Infrastructure 
{
    public abstract class AppConfig
    {
        public static readonly string ApplicationNames = Process.GetCurrentProcess().ProcessName;
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

            if (!hasConnectionData)
            {
                _connectionString = await ConnectivityService.ConnectivityChecker() ? $"Server={Ip};Port={Port};Database={Database};User Id={User};Password={Password};" : string.Empty;
            }
            else
            {
                _connectionString = $"Server={Ip};Port={Port};Database={Database};User Id={User};Password={Password};";
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
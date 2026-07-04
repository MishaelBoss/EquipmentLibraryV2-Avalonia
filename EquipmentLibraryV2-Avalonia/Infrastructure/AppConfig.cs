using System.Diagnostics;
using System.Threading.Tasks;
using EquipmentLibraryV2_Avalonia.Services;

namespace EquipmentLibraryV2_Avalonia.Infrastructure 
{
    public abstract class AppConfig
    {
        public static readonly string Applicationnames = Process.GetCurrentProcess().ProcessName;

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

            bool hasConnectionData = !string.IsNullOrWhiteSpace(Ip) &&
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
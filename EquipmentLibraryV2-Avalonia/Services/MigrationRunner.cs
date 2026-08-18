using System.Reflection;
using DbUp;
using Serilog;

namespace EquipmentLibraryV2_Avalonia.Services;

public static class MigrationRunner
{
    public static bool Run(string connectionString)
    {
        try
        {
            EnsureDatabase.For.PostgresqlDatabase(connectionString);

            var upgrader = DeployChanges.To
                .PostgresqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(
                    Assembly.GetExecutingAssembly(),
                    s => s.Contains(".Migrations."))
                .LogToConsole()
                .WithTransactionPerScript()
                .Build();

            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
            {
                Log.Error(result.Error, "Database migration failed");
                return false;
            }

            Log.Information("Database schema is up to date");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration failed");
            return false;
        }
    }
}
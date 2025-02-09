using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Helpers.Dbup;

namespace WeddingShare.Configurations
{
    internal static class DatabaseConfiguration
    {
        public static void AddDatabaseConfiguration(this IServiceCollection services, ConfigHelper config)
        {
            switch (config.GetOrDefault("Settings:Database:Type", "sqlite")?.ToLower())
            {
                case "sqlite":
                    services.AddSingleton<IDatabaseHelper, SQLiteDatabaseHelper>();
                    break;
                case "mysql":
                    services.AddSingleton<IDatabaseHelper, MySqlDatabaseHelper>();
                    break;
                default:
                    break;
            }

            services.AddHostedService<DbupMigrator>();
        }
    }
}
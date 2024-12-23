using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Helpers.Dbup;
using WeddingShare.Models.Database;

namespace WeddingShare.Configurations
{
    internal static class DatabaseConfiguration
    {
        public static void AddDatabaseConfiguration(this IServiceCollection services, ConfigHelper config)
        {
            switch (config.GetOrDefault("Database", "Database_Type", "sqlite")?.ToLower())
            {
                case "sqlite":
                    services.AddSingleton<IDatabaseHelper, SQLiteDatabaseHelper>();
                    break;
                default:
                    break;
            }

            services.AddHostedService<DbupMigrator>();
        }
    }
}
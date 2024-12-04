using System.Reflection;
using DbUp;
using DbUp.Engine;
using WeddingShare.Enums;

namespace WeddingShare.Helpers.Dbup
{
    public sealed class DbupMigrator(IEnvironmentWrapper environment, IConfiguration configuration, ILoggerFactory loggerFactory) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() =>
            {
                var logger = loggerFactory.CreateLogger<DbupMigrator>();

                if (!Directory.Exists("config"))
                { 
                    Directory.CreateDirectory("config");
                }

                var config = new ConfigHelper(environment, configuration, loggerFactory.CreateLogger<ConfigHelper>());
                var connString = config.GetOrDefault("Database", "Connection_String", "Data Source=./config/wedding-share.db");
                if (!string.IsNullOrWhiteSpace(connString))
                {
                    DatabaseUpgradeResult? dbupResult;

                    var dbType = config.GetOrDefault("Database", "Database_Type", "sqlite")?.ToLower();
                    switch (dbType)
                    {
                        case "sqlite":
                            dbupResult = new DbupSqliteHelper().Migrate(connString);
                            break;
                        default:
                            var error = $"Database type '{dbType}' is not yet supported by this application";
                            logger.LogWarning(error);
                            throw new NotImplementedException(error);
                    }

                    if (dbupResult != null && !dbupResult.Successful)
                    {
                        logger.LogWarning($"DBUP failed with error: '{dbupResult?.Error?.Message}' - '{dbupResult?.Error?.ToString()}'");
                    }
                }
                else
                {
                    logger.LogError($"DBUP failed with error: 'Connection string was null or empty'");
                    throw new ArgumentNullException("Please specify a valid database connection string");
                }
            }, stoppingToken);
        }
    }

    public class DbupSqliteHelper
    {
        public DatabaseUpgradeResult Migrate(string connectionString)
        {
            try
            {
                var dbupBuilder = DeployChanges.To
                    .SQLiteDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                    .WithScriptNameComparer(new DbupScriptComparer())
                    .WithFilter(new DbupScriptFilter(DatabaseType.SQLite))
                    .LogToConsole();
                dbupBuilder.Configure(c => c.Journal = new DbupTableJournal(() => c.ConnectionManager, () => c.Log, "schemaversions"));

                return dbupBuilder.Build().PerformUpgrade();
            }
            catch (Exception ex)
            {
                return new DatabaseUpgradeResult(null, false, ex, null);
            }
        }
    }
}
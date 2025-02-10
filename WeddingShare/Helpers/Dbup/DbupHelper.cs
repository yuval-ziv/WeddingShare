using System.Reflection;
using DbUp;
using DbUp.Engine;
using WeddingShare.Enums;
using WeddingShare.Helpers.Database;
using WeddingShare.Models.Database;

namespace WeddingShare.Helpers.Dbup
{
    public sealed class DbupMigrator(IEnvironmentWrapper environment, IConfiguration configuration, IDatabaseHelper database, IFileHelper fileHelper, ILoggerFactory loggerFactory) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var logger = loggerFactory.CreateLogger<DbupMigrator>();

            fileHelper.CreateDirectoryIfNotExists("config");

            var config = new ConfigHelper(environment, configuration, loggerFactory.CreateLogger<ConfigHelper>());
            var connString = config.GetOrDefault("Settings:Database:Connection_String", "Data Source=./config/wedding-share.db");
            if (!string.IsNullOrWhiteSpace(connString))
            {
                DatabaseUpgradeResult? dbupResult;

                var dbType = config.GetOrDefault("Settings:Database:Type", "sqlite")?.ToLower();
                switch (dbType)
                {
                    case "sqlite":
                        dbupResult = new DbupSqliteHelper().Migrate(connString);
                        break;
                    case "mysql":
                        dbupResult = new DbupMySqlHelper().Migrate(connString);
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

                var adminAccount = new UserModel() { Username = config.GetOrDefault("Settings:Account:Admin:Username", "admin"), Password = config.GetOrDefault("Settings:Account:Admin:Password", "admin") };
                await database.InitAdminAccount(adminAccount);
                    
                if (config.GetOrDefault("Settings:Account:Admin:Log_Password", true))
                {
                    logger.LogInformation($"Password: {adminAccount.Password}");
                }

                if (config.GetOrDefault("Security:2FA:Reset_To_Default", false))
                {
                    await database.ResetMultiFactorToDefault();
                }
            }
            else
            {
                logger.LogError($"DBUP failed with error: 'Connection string was null or empty'");
                throw new ArgumentNullException("Please specify a valid database connection string");
            }
        }
    }

    public class DbupSqliteHelper
    {
        public DatabaseUpgradeResult Migrate(string connectionString)
        {
            try
            {
                var dbupBuilder = DeployChanges.To
                    .SqliteDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                    .WithScriptNameComparer(new DbupScriptComparer())
                    .WithFilter(new DbupScriptFilter(DatabaseType.SQLite))
                    .LogToConsole();
                dbupBuilder.Configure(c => c.Journal = new DbupSQLiteTableJournal(() => c.ConnectionManager, () => c.Log, "schemaversions"));

                return dbupBuilder.Build().PerformUpgrade();
            }
            catch (Exception ex)
            {
                return new DatabaseUpgradeResult(null, false, ex, null);
            }
        }
    }

    public class DbupMySqlHelper
    {
        public DatabaseUpgradeResult Migrate(string connectionString)
        {
            try
            {
                var dbupBuilder = DeployChanges.To
                    .MySqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                    .WithScriptNameComparer(new DbupScriptComparer())
                    .WithFilter(new DbupScriptFilter(DatabaseType.MySQL))
                    .LogToConsole();
                dbupBuilder.Configure(c => c.Journal = new DbupMySqlTableJournal(() => c.ConnectionManager, () => c.Log, "weddingshare", "schemaversions"));

                return dbupBuilder.Build().PerformUpgrade();
            }
            catch (Exception ex)
            {
                return new DatabaseUpgradeResult(null, false, ex, null);
            }
        }
    }
}
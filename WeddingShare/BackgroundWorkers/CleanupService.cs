using NCrontab;
using WeddingShare.Helpers;

namespace WeddingShare.BackgroundWorkers
{
    public sealed class CleanupService(IWebHostEnvironment hostingEnvironment, IConfigHelper configHelper, IFileHelper fileHelper, ILogger<CleanupService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cron = configHelper.GetOrDefault("BackgroundServices:Schedules:Cleanup", "0 4 * * *");
            var schedule = CrontabSchedule.Parse(cron, new CrontabSchedule.ParseOptions() { IncludingSeconds = cron.Split(new[] { ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length == 6 });

            await Task.Delay((int)TimeSpan.FromSeconds(10).TotalMilliseconds, stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextExecutionTime = schedule.GetNextOccurrence(now);
                var waitTime = nextExecutionTime - now;
                await Task.Delay(waitTime, stoppingToken);

                await Cleanup();
            }
        }

        private async Task Cleanup()
        {
            await Task.Run(() =>
            {
                var paths = new List<string>()
                {
                    Path.Combine(hostingEnvironment.WebRootPath, "temp")
                };

                if (paths != null)
                { 
                    foreach (var path in paths)
                    { 
                        try
                        { 
                            fileHelper.DeleteDirectoryIfExists(path);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"An error occurred while running cleanup of '{path}'");
                        }
                    }
                }
            });
        }
    }
}
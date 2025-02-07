using System.Text;
using NCrontab;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Helpers.Notifications;

namespace WeddingShare.BackgroundWorkers
{
    public sealed class NotificationReport(IConfigHelper configHelper, IDatabaseHelper databaseHelper, ISmtpClientWrapper smtpHelper, ILoggerFactory loggerFactory) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (configHelper.GetOrDefault("Settings:Email_Report", true) && configHelper.GetOrDefault("Notifications:Smtp:Enabled", false))
            { 
                var cron = configHelper.GetOrDefault("BackgroundServices:Schedules:Email_Report", "0 0 * * *");
                var schedule = CrontabSchedule.Parse(cron, new CrontabSchedule.ParseOptions() { IncludingSeconds = cron.Split(new[] { ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length == 6 });

                while (!stoppingToken.IsCancellationRequested)
                {
                    var now = DateTime.Now;
                    var nextExecutionTime = schedule.GetNextOccurrence(now);
                    var waitTime = nextExecutionTime - now;
                    await Task.Delay(waitTime, stoppingToken);

                    await SendReport();
                }
            }
        }

        private async Task SendReport()
        {
            await Task.Run(async () =>
            {
                var pendingItems = await databaseHelper.GetPendingGalleryItems();
                if (pendingItems != null && pendingItems.Any())
                {
                    var builder = new StringBuilder();
                    builder.AppendLine($"<h1>You have items pending review!</h1>");
                    
                    foreach (var item in pendingItems.GroupBy(x => x.GalleryName).OrderBy(x => x.Key))
                    {
                        try
                        {
                            builder.AppendLine($"<p style=\"font-size: 16pt;\">{item.Key} - Pending Items ({item.Count()})</p>");
                        }
                        catch (Exception ex)
                        {
                            loggerFactory.CreateLogger<NotificationReport>().LogError(ex, $"Failed to build gallery report for id '{item?.Key}' - {ex?.Message}");
                        }
                    }

                    var sent = await new EmailHelper(configHelper, smtpHelper, loggerFactory.CreateLogger<EmailHelper>()).Send("Pending Items Report", builder.ToString());
                    if (!sent)
                    {
                        loggerFactory.CreateLogger<NotificationReport>().LogWarning($"Failed to send notification report");
                    }
                }
            });
        }
    }
}
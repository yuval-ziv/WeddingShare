using NCrontab;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using WeddingShare.Constants;
using WeddingShare.Enums;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Models.Database;

namespace WeddingShare.BackgroundWorkers
{
    public sealed class DirectoryScanner(IWebHostEnvironment hostingEnvironment, ISettingsHelper settingsHelper, IDatabaseHelper databaseHelper, IFileHelper fileHelper, IImageHelper imageHelper, ILogger<DirectoryScanner> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cron = settingsHelper.GetOrDefault(BackgroundServices.Schedules.DirectoryScanner, "*/30 * * * *").Result;
            var schedule = CrontabSchedule.Parse(cron, new CrontabSchedule.ParseOptions() { IncludingSeconds = cron.Split(new[] { ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length == 6 });

            await Task.Delay((int)TimeSpan.FromSeconds(10).TotalMilliseconds, stoppingToken);
            await ScanForFiles();

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextExecutionTime = schedule.GetNextOccurrence(now);
                var waitTime = nextExecutionTime - now;
                await Task.Delay(waitTime, stoppingToken);

                await ScanForFiles();
            }
        }

        private async Task ScanForFiles()
        {
            await Task.Run(async () =>
            {
                if (Startup.Ready)
                {
                    var thumbnailsDirectory = Path.Combine(hostingEnvironment.WebRootPath, "thumbnails");
                    fileHelper.CreateDirectoryIfNotExists(thumbnailsDirectory);

                    var uploadsDirectory = Path.Combine(hostingEnvironment.WebRootPath, "uploads");
                    if (fileHelper.DirectoryExists(uploadsDirectory))
                    {
                        var searchPattern = !settingsHelper.GetOrDefault(Settings.Basic.SingleGalleryMode, false).Result ? "*" : "default";
                        var galleries = fileHelper.GetDirectories(uploadsDirectory, searchPattern, SearchOption.TopDirectoryOnly)?.Where(x => !Path.GetFileName(x).StartsWith("."));
                        if (galleries != null)
                        {
                            foreach (var gallery in galleries)
                            {
                                try
                                {
                                    var id = Path.GetFileName(gallery).ToLower();
                                    var galleryItem = await databaseHelper.GetGallery(id);
                                    if (galleryItem == null)
                                    {
                                        if (await databaseHelper.GetGalleryCount() < await settingsHelper.GetOrDefault(Settings.Basic.MaxGalleryCount, 1000000))
                                        {
                                            galleryItem = await databaseHelper.AddGallery(new GalleryModel()
                                            {
                                                Name = id
                                            });
                                        }
                                    }

                                    if (galleryItem != null)
                                    {
                                        var allowedFileTypes = settingsHelper.GetOrDefault(Settings.Gallery.AllowedFileTypes, ".jpg,.jpeg,.png,.mp4,.mov", galleryItem?.Name).Result.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                        var galleryItems = await databaseHelper.GetAllGalleryItems(galleryItem.Id);

                                        if (Path.Exists(gallery))
                                        {
                                            var approvedFiles = fileHelper.GetFiles(gallery, "*.*", SearchOption.TopDirectoryOnly).Where(x => allowedFileTypes.Any(y => string.Equals(Path.GetExtension(x).Trim('.'), y.Trim('.'), StringComparison.OrdinalIgnoreCase)));
                                            if (approvedFiles != null)
                                            {
                                                foreach (var file in approvedFiles)
                                                {
                                                    try
                                                    {
                                                        var filename = Path.GetFileName(file);
                                                        var g = galleryItems.FirstOrDefault(x => string.Equals(x.Title, filename, StringComparison.OrdinalIgnoreCase));
                                                        if (g == null)
                                                        {
                                                            g = await databaseHelper.AddGalleryItem(new GalleryItemModel()
                                                            {
                                                                GalleryId = galleryItem.Id,
                                                                Title = filename,
                                                                Checksum = await fileHelper.GetChecksum(file),
                                                                MediaType = imageHelper.GetMediaType(file),
                                                                State = GalleryItemState.Approved,
                                                                UploadedDate = await fileHelper.GetCreationDatetime(file),
                                                                FileSize = fileHelper.FileSize(file),
                                                            });
                                                        }

                                                        var thumbnailPath = Path.Combine(thumbnailsDirectory, $"{Path.GetFileNameWithoutExtension(file)}.webp");
                                                        if (!fileHelper.FileExists(thumbnailPath))
                                                        {
                                                            await imageHelper.GenerateThumbnail(file, thumbnailPath, settingsHelper.GetOrDefault(Settings.Basic.ThumbnailSize, 720).Result);
                                                        }
                                                        else
                                                        {
                                                            using (var img = await Image.LoadAsync(thumbnailPath))
                                                            {
                                                                var width = img.Width;

                                                                img.Mutate(x => x.AutoOrient());

                                                                if (width != img.Width)
                                                                {
                                                                    await img.SaveAsWebpAsync(thumbnailPath);
                                                                }
                                                            }
                                                        }

                                                        if (g != null)
                                                        {
                                                            var updated = false;

                                                            if (g.UploadedDate == null)
                                                            {
                                                                g.UploadedDate = new FileInfo(file).CreationTimeUtc;
                                                                updated = true;
                                                            }

                                                            if (g.MediaType == MediaType.Unknown)
                                                            {
                                                                g.MediaType = imageHelper.GetMediaType(file);
                                                                updated = true;
                                                            }

                                                            if (g.Orientation == ImageOrientation.None)
                                                            {
                                                                g.Orientation = await imageHelper.GetOrientation(thumbnailPath);
                                                                updated = true;
                                                            }

                                                            if (g.FileSize == 0)
                                                            {
                                                                g.FileSize = fileHelper.FileSize(file);
                                                                updated = true;
                                                            }

                                                            if (updated)
                                                            {   
                                                                await databaseHelper.EditGalleryItem(g);
                                                            }
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        logger.LogError(ex, $"An error occurred while scanning file '{file}'");
                                                    }
                                                }
                                            }

                                            if (Path.Exists(Path.Combine(gallery, "Pending")))
                                            {
                                                var pendingFiles = fileHelper.GetFiles(Path.Combine(gallery, "Pending"), "*.*", SearchOption.TopDirectoryOnly).Where(x => allowedFileTypes.Any(y => string.Equals(Path.GetExtension(x).Trim('.'), y.Trim('.'), StringComparison.OrdinalIgnoreCase)));
                                                if (pendingFiles != null)
                                                {
                                                    foreach (var file in pendingFiles)
                                                    {
                                                        try
                                                        {
                                                            var filename = Path.GetFileName(file);
                                                            if (!galleryItems.Exists(x => string.Equals(x.Title, filename, StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                await databaseHelper.AddGalleryItem(new GalleryItemModel()
                                                                {
                                                                    GalleryId = galleryItem.Id,
                                                                    Title = filename,
                                                                    Checksum = await fileHelper.GetChecksum(file),
                                                                    MediaType = imageHelper.GetMediaType(file),
                                                                    State = GalleryItemState.Pending,
                                                                    UploadedDate = await fileHelper.GetCreationDatetime(file),
                                                                    FileSize = new FileInfo(file).Length
                                                                });
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            logger.LogError(ex, $"An error occurred while scanning file '{file}'");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, $"An error occurred while scanning directory '{gallery}'");
                                }
                            }
                        }
                    }
                }
                else
                { 
                    logger.LogInformation($"Skipping directory scan, application not ready yet");
                }
            });
        }
    }
}
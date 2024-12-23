using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using WeddingShare.Enums;

namespace WeddingShare.Helpers
{
    public interface IImageHelper
    {
        Task<bool> GenerateThumbnail(string imagePath, string savePath, int size = 720);
        ImageOrientation GetOrientation(Image img);
    }

    public class ImageHelper : IImageHelper
    {
        private readonly IFileHelper _fileHelper;
        private readonly ILogger _logger;

        public ImageHelper(IFileHelper fileHelper, ILogger<ImageHelper> logger)
        {
            _fileHelper = fileHelper;
            _logger = logger;
        }

        public async Task<bool> GenerateThumbnail(string imagePath, string savePath, int size = 720)
        {
            if (_fileHelper.FileExists(imagePath))
            { 
                try
                {
                    var filename = Path.GetFileName(imagePath);
                    using (var img = await Image.LoadAsync(imagePath))
                    {
                        var width = 0;
                        var height = 0;

                        var orientation = this.GetOrientation(img);
                        if (orientation == ImageOrientation.Square)
                        {
                            width = size;
                            height = size;
                        }
                        else if (orientation == ImageOrientation.Landscape)
                        {
                            var scale = (decimal)size / (decimal)img.Width;
                            width = (int)((decimal)img.Width * scale);
                            height = (int)((decimal)img.Height * scale);
                        }
                        else if (orientation == ImageOrientation.Portrait)
                        {
                            var scale = (decimal)size / (decimal)img.Height;
                            width = (int)((decimal)img.Width * scale);
                            height = (int)((decimal)img.Height * scale);
                        }

                        img.Mutate(x =>
                        {
                            x.Resize(width, height);
                            x.AutoOrient();
                        });

                        await img.SaveAsWebpAsync(savePath);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to generate thumbnail - '{imagePath}'");
                }
            }

            return false;
        }

        public ImageOrientation GetOrientation(Image img)
        {
            if (img != null)
            {
                if (img.Width > img.Height)
                {
                    return ImageOrientation.Landscape;
                }
                else if (img.Width < img.Height)
                {
                    return ImageOrientation.Portrait;
                }
                else if (img.Width == img.Height)
                {
                    return ImageOrientation.Square;
                }
            }

            return ImageOrientation.None;
        }
    }
}
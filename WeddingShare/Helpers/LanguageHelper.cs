using System.Globalization;

namespace WeddingShare.Helpers
{
    public interface ILanguageHelper
    {
        public List<CultureInfo> DetectSupportedCultures();
        public Task<List<CultureInfo>> DetectSupportedCulturesAsync();
    }

    public class LanguageHelper : ILanguageHelper
    {
        public List<CultureInfo> DetectSupportedCultures()
        {
            var supportedCultures = new List<CultureInfo>();

            try
            {
                var resourceFiles = Directory.GetFiles(Path.Combine("Resources", "Lang"), "*.resx");
                var detectedCultures = resourceFiles
                    .Select(x => Path.GetFileNameWithoutExtension(x))
                    .Where(x => x.Contains("."))
                    .Select(x => x.Split('.').LastOrDefault());

                foreach (var detectedCulture in detectedCultures)
                {
                    if (!string.IsNullOrWhiteSpace(detectedCulture))
                    {
                        try
                        {
                            supportedCultures.Add(new CultureInfo(detectedCulture));
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                supportedCultures.Add(new CultureInfo("en-GB"));
            }

            return supportedCultures;
        }

        public Task<List<CultureInfo>> DetectSupportedCulturesAsync()
        {
            return Task.Run(DetectSupportedCultures);
        }
    }
}
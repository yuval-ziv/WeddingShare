using System.Text.RegularExpressions;

namespace WeddingShare.Helpers
{
    public class UrlHelper
    {
        public static string Generate(HttpContext? ctx, IConfigHelper config, string? append)
        {
            return Generate(ctx?.Request, config, append);
        }

        public static string Generate(HttpRequest? ctx, IConfigHelper config, string? append)
        {
            if (ctx != null)
            {
                var scheme = config.GetOrDefault("Settings", "Force_Https", false) ? "https" : ctx.Scheme;
                var host = Regex.Replace(config.GetOrDefault("Settings", "Base_Url", ctx.Host.Value), "http[s]*\\:\\/\\/", string.Empty).TrimEnd('/');

                return $"{scheme}://{host}/{append?.TrimStart('/')}";
            }

            return string.Empty;
        }
    }
}
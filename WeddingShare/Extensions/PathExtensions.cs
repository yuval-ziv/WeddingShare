namespace WeddingShare.Extensions
{
    public static class PathExtensions
    {
        public static string ReplaceSeparator(this string path, string separator)
        {
            return path.Replace(new[] { "\\", "/" }, separator);
        }
    }
}
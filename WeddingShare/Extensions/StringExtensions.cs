namespace WeddingShare.Extensions
{
    public static class StringExtensions
    {
        public static string Replace(this string value, string[] oldChars, string newChar)
        {
            foreach (var s in oldChars)
            {
                if (!string.Equals(s, newChar))
                { 
                    value = value.Replace(s, newChar);
                }
            }

            return value;
        }
    }
}
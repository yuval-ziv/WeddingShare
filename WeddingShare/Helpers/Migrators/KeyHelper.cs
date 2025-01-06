using WeddingShare.Extensions;

namespace WeddingShare.Helpers.Migrators
{
    public class KeyHelper
    {
        public static string[] GetAlternateVersions(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                key = key.ToUpper();

                if (key.StartsWith("SETTINGS:ACCOUNT:ADMIN:", StringComparison.OrdinalIgnoreCase))
                {
                    return [key, key.Replace("SETTINGS:ACCOUNT:ADMIN:", "SETTINGS:ADMIN:")];
                }

                return [key];
            }
            else 
            {
                throw new ArgumentNullException("Key cannot be null or empty");
            }
        }
    }
}
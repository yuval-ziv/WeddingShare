using Microsoft.CodeAnalysis.CSharp.Syntax;
using WeddingShare.Models.Migrator;

namespace WeddingShare.Helpers.Migrators
{
    public class KeyHelper
    {
        public static List<KeyMigrator> GetAlternateVersions(string key, string? galleryId = null)
        {
            var keys = new List<KeyMigrator>();

            try
            {
                key = key.Trim();
                keys.Add(new KeyMigrator(1, key));

                if (string.Equals(key, "Settings:Home_Link", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Disable_Home_Link", (v) => { return (bool.Parse(v) == false).ToString(); }));
                }
                else if (string.Equals(key, "Settings:Disable_Guest_Gallery_Creation", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Guest_Gallery_Creation", (v) => { return (bool.Parse(v) == false).ToString(); }));
                }
                else if (string.Equals(key, "Settings:Themes:Enabled", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Disable_Dark_Mode", (v) => { return (bool.Parse(v) == false).ToString(); }));
                }

                else if (string.Equals(key, "Settings:Themes:Enabled", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Disable_Themes", (v) => { return (bool.Parse(v) == false).ToString(); }));
                }
                else if (string.Equals(key, "Settings:Themes:Default", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Default_Theme"));
                }

                else if (string.Equals(key, "Settings:Identity_Check:Enabled", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Show_Identity_Request"));
                }
                else if (string.Equals(key, "Settings:Show_Identity_Request", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Identity_Check:Enabled", (v) => { return (bool.Parse(v) == false).ToString(); }));
                }

                else if (string.Equals(key, "Settings:Account:Admins:Username", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Admin:Username"));
                }
                else if (string.Equals(key, "Settings:Account:Admins:Password", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Admin:Password"));
                }
                else if (string.Equals(key, "Settings:Account:Admins:Log_Password", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Admin:Log_Password"));
                }
                else if (string.Equals(key, "Settings:Gallery:QR_Code", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Disable_QR_Code", (v) => { return (bool.Parse(v) == false).ToString(); }));
                }
                else if (string.Equals(key, "Settings:Gallery:Secret_Key", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Secret_Key"));
                }
                else if (string.Equals(key, "Settings:Gallery:Columns", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Gallery_Columns"));
                }
                else if (string.Equals(key, "Settings:Gallery:Items_Per_Page", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Gallery_Items_Per_Page"));
                }
                else if (string.Equals(key, "Settings:Gallery:Quote", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Gallery_Quote"));
                }
                else if (string.Equals(key, "Settings:Gallery:Retain_Rejected_Items", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Retain_Rejected_Items"));
                }
                else if (string.Equals(key, "Settings:Gallery:Full_Width", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Full_Width_Gallery"));
                }
                else if (string.Equals(key, "Settings:Gallery:Upload", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Disable_Upload", (v) => { return (bool.Parse(v) == false).ToString(); }));
                }
                else if (string.Equals(key, "Settings:Gallery:Download", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Disable_Download", (v) => { return (bool.Parse(v) == false).ToString(); }));
                }
                else if (string.Equals(key, "Settings:Gallery:Require_Review", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Require_Review"));
                }
                else if (string.Equals(key, "Settings:Gallery:Review_Counter", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Disable_Review_Counter"));
                }
                else if (string.Equals(key, "Settings:Gallery:Prevent_Duplicates", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Prevent_Duplicates"));
                }
                else if (string.Equals(key, "Settings:Gallery:Idle_Refresh_Mins", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Idle_Gallery_Refresh_Mins"));
                }
                else if (string.Equals(key, "Settings:Gallery:Max_Size_Mb", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Max_Size_Mb"));
                }
                else if (string.Equals(key, "Settings:Gallery:Max_File_Size_Mb", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Max_File_Size_Mb"));
                }
                else if (string.Equals(key, "Settings:Gallery:Allowed_File_Types", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Allowed_File_Types"));
                }
                else if (string.Equals(key, "Settings:Gallery:Default_View", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Settings:Default_Gallery_View"));
                }

                else if (string.Equals(key, "BackgroundServices:Schedules:Directory_Scanner", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "BackgroundServices:Directory_Scanner_Interval"));
                }
                else if (string.Equals(key, "BackgroundServices:Schedules:Email_Report", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "BackgroundServices:Email_Report_Interval"));
                }
                else if (string.Equals(key, "BackgroundServices:Schedules:Cleanup", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "BackgroundServices:Cleanup_Interval"));
                }

                else if (string.Equals(key, "Security:Headers:Enabled", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Security:Set_Headers"));
                }
                else if (string.Equals(key, "Security:Headers:X_Frame_Options", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Security:X_Frame_Options"));
                }
                else if (string.Equals(key, "Security:Headers:X_Content_Type_Options", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Security:X_Content_Type_Options"));
                }
                else if (string.Equals(key, "Security:Headers:CSP", StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(new KeyMigrator(2, "Security:CSP_Header"));
                }

                if (!string.IsNullOrWhiteSpace(galleryId) && keys.Any())
                {
                    var priority = keys.Max(k => k.Priority) * -1;
                    var count = keys.Count;

                    for (var i = 0; i < count; i++)
                    {
                        var k = keys[i];
                        keys.Add(new KeyMigrator(priority + k.Priority, $"{k.Key}_{galleryId}", k.MigrationAction));
                    }
                }
            }
            catch { }

            keys = keys.Distinct().OrderBy(x => x.Priority).ToList();

            return keys;
        }
    }
}
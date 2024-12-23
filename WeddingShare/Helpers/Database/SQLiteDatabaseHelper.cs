using System.Data;
using Microsoft.Data.Sqlite;
using WeddingShare.Enums;
using WeddingShare.Models.Database;

namespace WeddingShare.Helpers.Database
{
    public class SQLiteDatabaseHelper : IDatabaseHelper
    {
        private readonly string _connString;
        private readonly ILogger _logger;

        public SQLiteDatabaseHelper(IConfigHelper config, ILogger<SQLiteDatabaseHelper> logger)
        {
            _connString = config.GetOrDefault("Database", "Connection_String", "Data Source=./config/wedding-share.db");
            _logger = logger;

            _logger.LogInformation($"Using SQLite connection string: '{_connString}'");
        }

        #region Gallery
        public async Task<List<GalleryModel>> GetAllGalleries()
        {
            List<GalleryModel> result;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` GROUP BY g.`id` ORDER BY `name` ASC;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);

                await conn.OpenAsync();
                result = await ReadGalleries(await cmd.ExecuteReaderAsync());
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryModel?> GetGallery(int id)
        {
            GalleryModel? result;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` WHERE g.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);

                await conn.OpenAsync();
                result = (await ReadGalleries(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryModel?> GetGallery(string name)
        {
            GalleryModel? result;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` WHERE g.`name`=@Name;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Name", name?.ToLower());
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);

                await conn.OpenAsync();
                result = (await ReadGalleries(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryModel?> AddGallery(GalleryModel model)
        {
            GalleryModel? result = null;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"INSERT INTO `galleries` (`name`, `secret_key`) VALUES (@Name, @SecretKey); SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` WHERE g.`id`=last_insert_rowid();", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Name", model.Name.ToLower());
                cmd.Parameters.AddWithValue("SecretKey", !string.IsNullOrWhiteSpace(model.SecretKey) ? model.SecretKey : DBNull.Value);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (await ReadGalleries(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryModel?> EditGallery(GalleryModel model)
        {
            GalleryModel? result = null;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"UPDATE `galleries` SET `name`=@Name, `secret_key`=@SecretKey WHERE `id`=@Id; SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` WHERE g.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Name", model.Name?.ToLower());
                cmd.Parameters.AddWithValue("SecretKey", !string.IsNullOrWhiteSpace(model.SecretKey) ? model.SecretKey : DBNull.Value);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (await ReadGalleries(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> WipeGallery(GalleryModel model)
        {
            bool result = false;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"DELETE FROM `gallery_items` WHERE `gallery_id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> WipeAllGalleries()
        {
            bool result = false;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"DELETE FROM `gallery_items`; DELETE FROM `galleries` WHERE `id` > 1;", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> DeleteGallery(GalleryModel model)
        {
            bool result = false;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"DELETE FROM `gallery_items` WHERE `gallery_id`=@Id; DELETE FROM `galleries` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }
        #endregion

        #region Gallery Items
        public async Task<List<GalleryItemModel>> GetAllGalleryItems(int galleryId, GalleryItemState state = GalleryItemState.All)
        {
            List<GalleryItemModel> result;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT * FROM `gallery_items` WHERE `gallery_id`=@Id {(state != GalleryItemState.All ? "AND `state`=@State" : string.Empty)} ORDER BY `id` ASC;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", galleryId);
                cmd.Parameters.AddWithValue("State", state);

                await conn.OpenAsync();
                result = await ReadGalleryItems(await cmd.ExecuteReaderAsync());
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<List<PendingGalleryItemModel>> GetPendingGalleryItems(int? galleryId = null)
        {
            List<PendingGalleryItemModel> result;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON g.`id` = gi.`gallery_id` WHERE gi.`state`=@State {(galleryId != null ? "AND gi.`gallery_id`=@GalleryId" : string.Empty)};", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("GalleryId", galleryId);
                cmd.Parameters.AddWithValue("State", (int)GalleryItemState.Pending);

                await conn.OpenAsync();
                result = await ReadPendingGalleryItems(await cmd.ExecuteReaderAsync());
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<PendingGalleryItemModel?> GetPendingGalleryItem(int id)
        {
            PendingGalleryItemModel? result;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON g.`id` = gi.`gallery_id` WHERE gi.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);

                await conn.OpenAsync();
                result = (await ReadPendingGalleryItems(await cmd.ExecuteReaderAsync())).FirstOrDefault();
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<int> GetPendingGalleryItemCount(int? galleryId = null)
        {
            int result = 0;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT COUNT(`id`) FROM `gallery_items` {(galleryId != null ? "WHERE `gallery_id`=@GalleryId" : string.Empty)};", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("GalleryId", galleryId);


                await conn.OpenAsync();
                result = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryItemModel?> GetGalleryItem(int id)
        {
            GalleryItemModel? result;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT * FROM `gallery_items` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);

                await conn.OpenAsync();
                result = (await ReadGalleryItems(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryItemModel?> AddGalleryItem(GalleryItemModel model)
        {
            GalleryItemModel? result = null;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"INSERT INTO `gallery_items` (`gallery_id`, `title`, `state`, `uploaded_by`) VALUES (@GalleryId, @Title, @State, @UploadedBy); SELECT * FROM `gallery_items` WHERE `id`=last_insert_rowid();", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("GalleryId", model.GalleryId);
                cmd.Parameters.AddWithValue("Title", model.Title);
                cmd.Parameters.AddWithValue("State", (int)model.State);
                cmd.Parameters.AddWithValue("UploadedBy", !string.IsNullOrWhiteSpace(model.UploadedBy) ? model.UploadedBy : DBNull.Value);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (await ReadGalleryItems(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryItemModel?> EditGalleryItem(GalleryItemModel model)
        {
            GalleryItemModel? result = null;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"UPDATE `gallery_items` SET `title`=@Title, `state`=@State, `uploaded_by`=@UploadedBy WHERE `id`=@Id; SELECT * FROM `gallery_items` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Title", model.Title);
                cmd.Parameters.AddWithValue("State", (int)model.State);
                cmd.Parameters.AddWithValue("UploadedBy", !string.IsNullOrWhiteSpace(model.UploadedBy) ? model.UploadedBy : DBNull.Value);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (await ReadGalleryItems(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> DeleteGalleryItem(GalleryItemModel model)
        {
            bool result = false;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"DELETE FROM `gallery_items` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }
        #endregion

        #region Users
        public async Task<bool> InitAdminAccount(UserModel model)
        {
            bool result = false;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"UPDATE `users` SET `username`=@Username, `password`=@Password WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", 1);
                cmd.Parameters.AddWithValue("Username", model.Username.ToLower());
                cmd.Parameters.AddWithValue("Password", model.Password);

                await conn.OpenAsync();
                result = await cmd.ExecuteNonQueryAsync() > 0;
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> ValidateCredentials(string username, string password)
        {
            bool result = false;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT COUNT(`id`) FROM `users` WHERE `username`=@Username AND `password`=@Password;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", 1);
                cmd.Parameters.AddWithValue("Username", username.ToLower());
                cmd.Parameters.AddWithValue("Password", password);

                await conn.OpenAsync();
                result = (long)(await cmd.ExecuteScalarAsync() ?? 0) > 0;
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> GetUser(int id)
        {
            UserModel? result;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT * FROM `users` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);

                await conn.OpenAsync();
                result = (await ReadUsers(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> GetUser(string username)
        {
            UserModel? result;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"SELECT * FROM `users` WHERE `username`=@Username;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Username", username.ToLower());

                await conn.OpenAsync();
                result = (await ReadUsers(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> AddUser(UserModel model)
        {
            UserModel? result = null;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"INSERT INTO `users` (`username`, `email`, `password`) VALUES (@Username, @Email, @Password); SELECT SELECT * FROM `users` WHERE `id`=last_insert_rowid();", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Username", model.Username.ToLower());
                cmd.Parameters.AddWithValue("Email", model.Email);
                cmd.Parameters.AddWithValue("Password", model.Password);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (await ReadUsers(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> EditUser(UserModel model)
        {
            UserModel? result = null;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"UPDATE `users` SET `username`=@Username, `email`=@Email, `failed_logins`=@FailedLogins, `lockout_until`=@LockoutUntil WHERE `id`=@Id; SELECT * FROM `users` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Username", model.Username.ToLower());
                cmd.Parameters.AddWithValue("Email", !string.IsNullOrEmpty(model.Email) ? model.Email : DBNull.Value);
                cmd.Parameters.AddWithValue("FailedLogins", model.FailedLogins);
                cmd.Parameters.AddWithValue("LockoutUntil", model.LockoutUntil != null ? ((DateTime)model.LockoutUntil - new DateTime(1970, 1, 1)).TotalSeconds : DBNull.Value);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (await ReadUsers(await cmd.ExecuteReaderAsync()))?.FirstOrDefault();
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> DeleteUser(UserModel model)
        {
            bool result = false;

            if (model.Id > 1 && !string.Equals("Admin", model.Username, StringComparison.OrdinalIgnoreCase))
            { 
                using (var conn = new SqliteConnection(_connString))
                {
                    var cmd = new SqliteCommand($"DELETE FROM `users` WHERE `id`=@Id;", conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("Id", model.Id);

                    await conn.OpenAsync();
                    var tran = await conn.BeginTransactionAsync();
                    try
                    {
                        cmd.Transaction = (SqliteTransaction)tran;
                        result = (await cmd.ExecuteNonQueryAsync()) > 0;
                        await tran.CommitAsync();
                    }
                    catch
                    {
                        await tran.RollbackAsync();
                    }

                    await conn.CloseAsync();
                }
            }

            return result;
        }

        public async Task<bool> ChangePassword(UserModel model)
        {
            bool result = false;

            using (var conn = new SqliteConnection(_connString))
            {
                var cmd = new SqliteCommand($"UPDATE `users` SET `password`=@Password WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Password", model.Password);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();
                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    result = (int)(await cmd.ExecuteScalarAsync() ?? 0) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<int> IncrementLockoutCount(int id)
        {
            int result = 0;

            var user = await this.GetUser(id);
            if (user != null)
            {
                user.FailedLogins++;
                result = (await this.EditUser(user))?.FailedLogins ?? 0;
            }

            return result;
        }

        public async Task<bool> SetLockout(int id, DateTime? datetime) 
        {
            bool result = false;

            using (var conn = new SqliteConnection(_connString))
            {
                if (datetime != null)
                {
                    var lockout = (DateTime)datetime;
                    datetime = new DateTime(lockout.Year, lockout.Month, lockout.Day, lockout.Hour, lockout.Minute, 0, DateTimeKind.Utc);
                }

                var cmd = new SqliteCommand($"UPDATE `users` SET `lockout_until`=@LockoutUntil WHERE `id`=@Id; SELECT `lockout_until` FROM `users` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("LockoutUntil", datetime != null ? ((DateTime)datetime - new DateTime(1970, 1, 1)).TotalSeconds : DBNull.Value);

                await conn.OpenAsync();
                var tran = await conn.BeginTransactionAsync();

                try
                {
                    cmd.Transaction = (SqliteTransaction)tran;
                    var reader = await cmd.ExecuteReaderAsync();
                    if (reader != null && reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                result = (!await reader.IsDBNullAsync("lockout_until") ? DateTime.UnixEpoch.AddSeconds(reader.GetInt32("lockout_until")) : null) == datetime;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"Failed to parse user lockout from database - {ex?.Message}");
                            }
                        }
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> ResetLockoutCount(int id)
        {
            bool result = false;

            var user = await this.GetUser(id);
            if (user != null)
            {
                user.FailedLogins = 0;
                result = ((await this.EditUser(user))?.FailedLogins ?? 0) == 0;
            }

            return result;
        }
        #endregion

        #region Backups
        public async Task<bool> Import(string path)
        {
            bool result = false;

            try
            {
                using (var backup = new SqliteConnection(path))
                using (var conn = new SqliteConnection(_connString))
                {
                    await backup.OpenAsync();
                    await conn.OpenAsync();

                    backup.BackupDatabase(conn);

                    await conn.CloseAsync();
                    await backup.CloseAsync();

                    SqliteConnection.ClearPool(backup);
                }

                result = true;
            }
            catch { }

            return result;
        }

        public async Task<bool> Export(string path)
        {
            bool result = false;

            try
            {
                using (var conn = new SqliteConnection(_connString))
                using (var backup = new SqliteConnection(path))
                {
                    await conn.OpenAsync();
                    await backup.OpenAsync();

                    conn.BackupDatabase(backup);

                    await backup.CloseAsync();
                    await conn.CloseAsync();
                
                    SqliteConnection.ClearPool(backup);
                }

                result = true;
            }
            catch { }

            return result;
        }
        #endregion

        #region Data Parsers
        private async Task<List<GalleryModel>> ReadGalleries(SqliteDataReader? reader)
        {
            var items = new List<GalleryModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        var id = !await reader.IsDBNullAsync("id") ? reader.GetInt32("id") : 0;
                        if (id > 0)
                        { 
                            items.Add(new GalleryModel()
                            {
                                Id = id,
                                Name = !await reader.IsDBNullAsync("name") ? reader.GetString("name") : null,
                                SecretKey = !await reader.IsDBNullAsync("secret_key") ? reader.GetString("secret_key") : null,
                                TotalItems = !await reader.IsDBNullAsync("total") ? reader.GetInt32("total") : 0,
                                ApprovedItems = !await reader.IsDBNullAsync("approved") ? reader.GetInt32("approved") : 0,
                                PendingItems = !await reader.IsDBNullAsync("pending") ? reader.GetInt32("pending") : 0,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse gallery model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }

        private async Task<List<GalleryItemModel>> ReadGalleryItems(SqliteDataReader? reader)
        {
            var items = new List<GalleryItemModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        var id = !await reader.IsDBNullAsync("id") ? reader.GetInt32("id") : 0;
                        if (id > 0)
                        { 
                            items.Add(new GalleryItemModel()
                            {
                                Id = id,
                                GalleryId = !await reader.IsDBNullAsync("gallery_id") ? reader.GetInt32("gallery_id") : 0,
                                Title = !await reader.IsDBNullAsync("title") ? reader.GetString("title") : string.Empty,
                                UploadedBy = !await reader.IsDBNullAsync("uploaded_by") ? reader.GetString("uploaded_by") : null,
                                State = !await reader.IsDBNullAsync("state") ? (GalleryItemState)reader.GetInt32("state") : GalleryItemState.Pending
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse gallery item model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }

        private async Task<List<PendingGalleryItemModel>> ReadPendingGalleryItems(SqliteDataReader? reader)
        {
            var items = new List<PendingGalleryItemModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {

                        var id = !await reader.IsDBNullAsync("id") ? reader.GetInt32("id") : 0;
                        if (id > 0)
                        { 
                            items.Add(new PendingGalleryItemModel()
                            {
                                Id = id,
                                GalleryId = !await reader.IsDBNullAsync("gallery_id") ? reader.GetInt32("gallery_id") : 0,
                                GalleryName = !await reader.IsDBNullAsync("gallery_name") ? reader.GetString("gallery_name") : "default",
                                Title = !await reader.IsDBNullAsync("title") ? reader.GetString("title") : string.Empty,
                                UploadedBy = !await reader.IsDBNullAsync("uploaded_by") ? reader.GetString("uploaded_by") : null,
                                State = !await reader.IsDBNullAsync("state") ? (GalleryItemState)reader.GetInt32("state") : GalleryItemState.Pending
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse pending gallery item model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }

        private async Task<List<UserModel>> ReadUsers(SqliteDataReader? reader)
        {
            var items = new List<UserModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        var id = !await reader.IsDBNullAsync("id") ? reader.GetInt32("id") : 0;
                        if (id > 0)
                        { 
                            items.Add(new UserModel()
                            {
                                Id = id,
                                Username = !await reader.IsDBNullAsync("failed_logins") ? reader.GetString("username").ToLower() : string.Empty,
                                Email = !await reader.IsDBNullAsync("email") ? reader.GetString("email") : null,
                                Password = null,
                                FailedLogins = !await reader.IsDBNullAsync("failed_logins") ? reader.GetInt32("failed_logins") : 0,
                                LockoutUntil = !await reader.IsDBNullAsync("lockout_until") ? DateTime.UnixEpoch.AddSeconds(reader.GetInt32("lockout_until")) : null
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse user model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }
        #endregion
    }
}
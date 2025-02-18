using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using WeddingShare.Enums;
using WeddingShare.Models.Database;

namespace WeddingShare.Helpers.Database
{
    public class MySqlDatabaseHelper : IDatabaseHelper
    {
        private readonly string _connString;
        private readonly ILogger _logger;

        public MySqlDatabaseHelper(IConfigHelper config, ILogger<MySqlDatabaseHelper> logger)
        {
            _connString = config.GetOrDefault("Settings:Database:Connection_String", "Server=mysql;Port=3306;Database=WeddingShare;Uid=WeddingShare;Pwd=Password;");
            _logger = logger;

            _logger.LogInformation($"Using MySQL connection string: '{_connString}'");
        }

        #region Setup
        private async Task<MySqlConnection> GetConnection()
        {
            return await GetConnection(_connString);
        }

        private async Task<MySqlConnection> GetConnection(string connString)
        {
            var conn = new MySqlConnection(connString);
            await DisableOnlyFullGroup(conn);
            return conn;
        }

        private MySqlCommand CreateCommand(string cmd, MySqlConnection conn)
        {
            return new MySqlCommand(cmd, conn);
        }

        private async Task<MySqlTransaction> CreateTransaction(MySqlConnection conn)
        {
            return (MySqlTransaction)await conn.BeginTransactionAsync();
        }

        private async Task<bool> DisableOnlyFullGroup(MySqlConnection conn)
        {
            var cmd = CreateCommand($"SET SESSION sql_mode=(SELECT REPLACE(@@sql_mode,'ONLY_FULL_GROUP_BY',''));", conn);
            cmd.CommandType = CommandType.Text;

            await conn.OpenAsync();
            var result = await cmd.ExecuteNonQueryAsync() > 0;
            await conn.CloseAsync();

            return result;
        }

        private void ClearPool(MySqlConnection conn)
        {
            MySqlConnection.ClearPool(conn);
        }
        #endregion

        #region Gallery
        public async Task<List<GalleryModel>> GetAllGalleries()
        {
            List<GalleryModel> result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` GROUP BY g.`id` ORDER BY `name` ASC;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                { 
                    result = await ReadGalleries(reader);
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryModel?> GetGallery(int id)
        {
            GalleryModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` {(id > 0 ? "WHERE g.`id`=@Id;" : string.Empty)}", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);

                await conn.OpenAsync();
                if (id > 0)
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadGalleries(reader))?.FirstOrDefault();
                    }
                }
                else
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var galleries = await ReadGalleries(reader);
                        result = new GalleryModel()
                        {
                            Id = 0,
                            Name = "all",
                            SecretKey = null,
                            TotalItems = galleries?.Sum(x => x.TotalItems) ?? 0,
                            ApprovedItems = galleries?.Sum(x => x.ApprovedItems) ?? 0,
                            PendingItems = galleries?.Sum(x => x.PendingItems) ?? 0
                        };
                    }
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryModel?> GetGallery(string name)
        {
            GalleryModel? result = null;

            if (string.Equals("All", name, StringComparison.OrdinalIgnoreCase))
            {
                result = await GetGallery(0);
            }
            else
            {
                long? galleryId = 0;
                using (var conn = await GetConnection())
                {
                    var cmd = CreateCommand($"SELECT `id` FROM `galleries` WHERE `name`=@Name;", conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("Name", name?.ToLower());

                    await conn.OpenAsync();
                    galleryId = (long?)await cmd.ExecuteScalarAsync();
                    await conn.CloseAsync();
                }

                if (galleryId != null && galleryId > 0)
                {
                    result = await GetGallery((int)galleryId);
                }
            }

            return result;
        }

        public async Task<GalleryModel?> AddGallery(GalleryModel model)
        {
            GalleryModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"INSERT INTO `galleries` (`name`, `secret_key`) VALUES (@Name, @SecretKey); SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` WHERE g.`id`=LAST_INSERT_ID();", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Name", model.Name.ToLower());
                cmd.Parameters.AddWithValue("SecretKey", !string.IsNullOrWhiteSpace(model.SecretKey) ? model.SecretKey : DBNull.Value);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadGalleries(reader))?.FirstOrDefault();
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

        public async Task<GalleryModel?> EditGallery(GalleryModel model)
        {
            GalleryModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `galleries` SET `name`=@Name, `secret_key`=@SecretKey WHERE `id`=@Id; SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` WHERE g.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Name", model.Name?.ToLower());
                cmd.Parameters.AddWithValue("SecretKey", !string.IsNullOrWhiteSpace(model.SecretKey) ? model.SecretKey : DBNull.Value);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadGalleries(reader))?.FirstOrDefault();
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

        public async Task<bool> WipeGallery(GalleryModel model)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"DELETE FROM `gallery_items` WHERE `gallery_id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
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

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"DELETE FROM `gallery_items`; DELETE FROM `galleries` WHERE `id` > 1;", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
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

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"DELETE FROM `gallery_items` WHERE `gallery_id`=@Id; DELETE FROM `galleries` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
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
        public async Task<List<GalleryItemModel>> GetAllGalleryItems(int? galleryId, GalleryItemState state = GalleryItemState.All, MediaType type = MediaType.All, GalleryOrder order = GalleryOrder.UploadedDesc, int limit = int.MaxValue, int page = 1)
        {
            List<GalleryItemModel> result;

            using (var conn = await GetConnection())
            {
                var query = $"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE {(galleryId != null && galleryId > 0 ? "gi.`gallery_id`=@Id" : "gi.`gallery_id` > 0")} {(type != MediaType.All ? "AND gi.`media_type`=@Type" : string.Empty)} {(state != GalleryItemState.All ? "AND gi.`state`=@State" : string.Empty)};";
                switch (order)
                {
                    case GalleryOrder.UploadedAsc:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`id` ASC;";
                        break;
                    case GalleryOrder.UploadedDesc:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`id` DESC;";
                        break;
                    case GalleryOrder.NameAsc:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`title` ASC;";
                        break;
                    case GalleryOrder.NameDesc:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`title` DESC;";
                        break;
                    case GalleryOrder.Random:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY RAND();";
                        break;
                    default:
                        break;
                }

                if (limit > 0 && page > 0)
                {
                    query = $"{query.TrimEnd(' ', ';')} LIMIT @Limit OFFSET @Offset;";
                }

                var cmd = CreateCommand(query, conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", galleryId);
                cmd.Parameters.AddWithValue("Type", type);
                cmd.Parameters.AddWithValue("State", state);
                cmd.Parameters.AddWithValue("Limit", limit);
                cmd.Parameters.AddWithValue("Offset", ((page - 1) * limit));

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = await ReadGalleryItems(reader);
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<List<GalleryItemModel>> GetPendingGalleryItems(int? galleryId = null)
        {
            List<GalleryItemModel> result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON g.`id` = gi.`gallery_id` WHERE gi.`state`=@State {(galleryId != null && galleryId > 0 ? "AND gi.`gallery_id`=@GalleryId" : string.Empty)};", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("GalleryId", galleryId);
                cmd.Parameters.AddWithValue("State", (int)GalleryItemState.Pending);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = await ReadPendingGalleryItems(reader);
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryItemModel?> GetPendingGalleryItem(int id)
        {
            GalleryItemModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON g.`id` = gi.`gallery_id` WHERE gi.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadPendingGalleryItems(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<int> GetPendingGalleryItemCount(int? galleryId = null)
        {
            int result = 0;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT COUNT(`id`) FROM `gallery_items` {(galleryId != null && galleryId > 0 ? "WHERE `gallery_id`=@GalleryId" : string.Empty)};", conn);
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

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id`  WHERE gi.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadGalleryItems(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryItemModel?> GetGalleryItemByChecksum(int galleryId, string checksum)
        {
            GalleryItemModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id`  WHERE g.`id`=@Id AND gi.`checksum`=@Checksum;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", galleryId);
                cmd.Parameters.AddWithValue("Checksum", checksum);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadGalleryItems(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryItemModel?> AddGalleryItem(GalleryItemModel model)
        {
            GalleryItemModel? result = null;

            if (model.GalleryId > 0)
            {
                using (var conn = await GetConnection())
                {
                    var cmd = CreateCommand($"INSERT INTO `gallery_items` (`gallery_id`, `title`, `state`, `uploaded_by`, `checksum`, `media_type`) VALUES (@GalleryId, @Title, @State, @UploadedBy, @Checksum, @MediaType); SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE gi.`id`=LAST_INSERT_ID();", conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("GalleryId", model.GalleryId);
                    cmd.Parameters.AddWithValue("Title", model.Title);
                    cmd.Parameters.AddWithValue("State", (int)model.State);
                    cmd.Parameters.AddWithValue("UploadedBy", !string.IsNullOrWhiteSpace(model.UploadedBy) ? model.UploadedBy : DBNull.Value);
                    cmd.Parameters.AddWithValue("Checksum", !string.IsNullOrWhiteSpace(model.Checksum) ? model.Checksum : DBNull.Value);
                    cmd.Parameters.AddWithValue("MediaType", (int)model.MediaType);

                    await conn.OpenAsync();
                    var tran = await CreateTransaction(conn);

                    try
                    {
                        cmd.Transaction = tran;
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            result = (await ReadGalleryItems(reader))?.FirstOrDefault();
                        }
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

        public async Task<GalleryItemModel?> EditGalleryItem(GalleryItemModel model)
        {
            GalleryItemModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `gallery_items` SET `title`=@Title, `state`=@State, `uploaded_by`=@UploadedBy, `checksum`=@Checksum WHERE `id`=@Id; SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE gi.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Title", model.Title);
                cmd.Parameters.AddWithValue("State", (int)model.State);
                cmd.Parameters.AddWithValue("UploadedBy", !string.IsNullOrWhiteSpace(model.UploadedBy) ? model.UploadedBy : DBNull.Value);
                cmd.Parameters.AddWithValue("Checksum", !string.IsNullOrWhiteSpace(model.Checksum) ? model.Checksum : DBNull.Value);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadGalleryItems(reader))?.FirstOrDefault();
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

        public async Task<bool> DeleteGalleryItem(GalleryItemModel model)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"DELETE FROM `gallery_items` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
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

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `users` SET `username`=@Username, `password`=@Password WHERE `id`=@Id;", conn);
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

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT COUNT(`id`) FROM `users` WHERE `username`=@Username AND `password`=@Password;", conn);
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

        public async Task<List<UserModel>?> GetAllUsers()
        {
            List<UserModel> result = new List<UserModel>();

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT * FROM `users` ORDER BY UPPER(`username`) ASC;", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadUsers(reader));
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> GetUser(int id)
        {
            UserModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT * FROM `users` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadUsers(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> GetUser(string username)
        {
            UserModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT * FROM `users` WHERE `username`=@Username;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Username", username.ToLower());

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadUsers(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> AddUser(UserModel model)
        {
            UserModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"INSERT INTO `users` (`username`, `email`, `password`) VALUES (@Username, @Email, @Password); SELECT * FROM `users` WHERE `id`=LAST_INSERT_ID();", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Username", model.Username.ToLower());
                cmd.Parameters.AddWithValue("Email", !string.IsNullOrEmpty(model.Email) ? model.Email : DBNull.Value);
                cmd.Parameters.AddWithValue("Password", model.Password);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadUsers(reader))?.FirstOrDefault();
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

        public async Task<UserModel?> EditUser(UserModel model)
        {
            UserModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `users` SET `username`=@Username, `email`=@Email, `failed_logins`=@FailedLogins, `lockout_until`=@LockoutUntil WHERE `id`=@Id; SELECT * FROM `users` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Username", model.Username.ToLower());
                cmd.Parameters.AddWithValue("Email", !string.IsNullOrEmpty(model.Email) ? model.Email : DBNull.Value);
                cmd.Parameters.AddWithValue("FailedLogins", model.FailedLogins);
                cmd.Parameters.AddWithValue("LockoutUntil", model.LockoutUntil != null ? ((DateTime)model.LockoutUntil - new DateTime(1970, 1, 1)).TotalSeconds : DBNull.Value);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadUsers(reader))?.FirstOrDefault();
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

        public async Task<bool> DeleteUser(UserModel model)
        {
            bool result = false;

            if (model.Id > 1 && !string.Equals("Admin", model.Username, StringComparison.OrdinalIgnoreCase))
            {
                using (var conn = await GetConnection())
                {
                    var cmd = CreateCommand($"DELETE FROM `users` WHERE `id`=@Id;", conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("Id", model.Id);

                    await conn.OpenAsync();
                    var tran = await CreateTransaction(conn);

                    try
                    {
                        cmd.Transaction = tran;
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

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `users` SET `password`=@Password WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Password", model.Password);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
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

            using (var conn = await GetConnection())
            {
                if (datetime != null)
                {
                    var lockout = (DateTime)datetime;
                    datetime = new DateTime(lockout.Year, lockout.Month, lockout.Day, lockout.Hour, lockout.Minute, 0, DateTimeKind.Utc);
                }

                var cmd = CreateCommand($"UPDATE `users` SET `lockout_until`=@LockoutUntil WHERE `id`=@Id; SELECT `lockout_until` FROM `users` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("LockoutUntil", datetime != null ? ((DateTime)datetime - new DateTime(1970, 1, 1)).TotalSeconds : DBNull.Value);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    { 
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

        public async Task<bool> SetMultiFactorToken(int id, string token)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `users` SET `2fa_token`=@Token WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("Token", token);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = await cmd.ExecuteNonQueryAsync() > 0;
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

        public async Task<bool> ResetMultiFactorToDefault()
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `users` SET `2fa_token`='';", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = await cmd.ExecuteNonQueryAsync() > 0;
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

        #region Backups
        public async Task<bool> Import(string path)
        {
            throw new NotImplementedException("This feature is not yet available");
        }

        public async Task<bool> Export(string path)
        {
            throw new NotImplementedException("This feature is not yet available");
        }
        #endregion

        #region Data Parsers
        private async Task<List<GalleryModel>> ReadGalleries(DbDataReader? reader)
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
                                Name = !await reader.IsDBNullAsync("name") ? reader.GetString("name") : "Unknown",
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

        private async Task<List<GalleryItemModel>> ReadGalleryItems(DbDataReader? reader)
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
                                GalleryName = !await reader.IsDBNullAsync("gallery_name") ? reader.GetString("gallery_name") : string.Empty,
                                Title = !await reader.IsDBNullAsync("title") ? reader.GetString("title") : string.Empty,
                                UploadedBy = !await reader.IsDBNullAsync("uploaded_by") ? reader.GetString("uploaded_by") : null,
                                Checksum = !await reader.IsDBNullAsync("checksum") ? reader.GetString("checksum") : null,
                                MediaType = !await reader.IsDBNullAsync("media_type") ? (MediaType)reader.GetInt32("media_type") : MediaType.Unknown,
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

        private async Task<List<GalleryItemModel>> ReadPendingGalleryItems(DbDataReader? reader)
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
                                GalleryName = !await reader.IsDBNullAsync("gallery_name") ? reader.GetString("gallery_name") : "default",
                                Title = !await reader.IsDBNullAsync("title") ? reader.GetString("title") : string.Empty,
                                UploadedBy = !await reader.IsDBNullAsync("uploaded_by") ? reader.GetString("uploaded_by") : null,
                                Checksum = !await reader.IsDBNullAsync("checksum") ? reader.GetString("checksum") : null,
                                MediaType = !await reader.IsDBNullAsync("media_type") ? (MediaType)reader.GetInt32("media_type") : MediaType.Unknown,
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

        private async Task<List<UserModel>> ReadUsers(DbDataReader? reader)
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
                                LockoutUntil = !await reader.IsDBNullAsync("lockout_until") ? DateTime.UnixEpoch.AddSeconds(reader.GetInt32("lockout_until")) : null,
                                MultiFactorToken = !await reader.IsDBNullAsync("2fa_token") ? reader.GetString("2fa_token") : null
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
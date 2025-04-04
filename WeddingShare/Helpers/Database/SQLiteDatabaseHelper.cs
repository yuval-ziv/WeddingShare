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
            _connString = config.GetOrDefault("Settings:Database:Connection_String", "Data Source=./config/wedding-share.db");
            _logger = logger;

            _logger.LogInformation($"Using SQLite connection string: '{_connString}'");
        }

        #region Setup
        private async Task<SqliteConnection> GetConnection()
        {
            return await GetConnection(_connString);
        }

        private async Task<SqliteConnection> GetConnection(string connString)
        {
            return await Task.Run(() => { return new SqliteConnection(connString); });
        }

        private SqliteCommand CreateCommand(string cmd, SqliteConnection conn)
        {
            return new SqliteCommand(cmd, conn);
        }

        private async Task<SqliteTransaction> CreateTransaction(SqliteConnection conn)
        {
            return (SqliteTransaction)await conn.BeginTransactionAsync();
        }

        private void ClearPool(SqliteConnection conn)
        {
            SqliteConnection.ClearPool(conn);
        }
        #endregion

        #region Gallery
        public async Task<IEnumerable<string>> GetGalleryNames()
        {
            List<string> result = new List<string>();

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.`name` FROM `galleries` AS g ORDER BY `name` ASC;", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader != null && reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                var name = !await reader.IsDBNullAsync("name") ? reader.GetString("name") : null;
                                if (!string.IsNullOrWhiteSpace(name))
                                {
                                    result.Add(name);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"Failed to parse gallery model from database - {ex?.Message}");
                            }
                        }
                    }
                }
                await conn.CloseAsync();
            }

            return result;
        }

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
                var cmd = CreateCommand($"INSERT INTO `galleries` (`name`, `secret_key`) VALUES (@Name, @SecretKey); SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` WHERE g.`id`=last_insert_rowid();", conn);
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
        public async Task<IDictionary<string, long>> GetGalleryItemCount(int? galleryId, GalleryItemState state = GalleryItemState.All, MediaType type = MediaType.All, ImageOrientation orientation = ImageOrientation.None)
        {
            var results = new Dictionary<string, long>();

            using (var conn = await GetConnection())
            {
                var query = $"SELECT `state`, COUNT(gi.`id`) AS `count` FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE {(galleryId != null && galleryId > 0 ? "gi.`gallery_id`=@Id" : "gi.`gallery_id` > 0")}{(type != MediaType.All ? " AND gi.`media_type`=@Type" : string.Empty)}{(orientation != ImageOrientation.None ? " AND gi.`orientation`=@Orientation" : string.Empty)}{(state != GalleryItemState.All ? " AND gi.`state`=@State" : string.Empty)} GROUP BY `state`;";

                var cmd = CreateCommand(query, conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", galleryId);
                cmd.Parameters.AddWithValue("Type", type);
                cmd.Parameters.AddWithValue("Orientation", orientation);
                cmd.Parameters.AddWithValue("State", state);

                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                if (reader != null && reader.HasRows)
                {
                    while (reader.Read())
                    {
                        try
                        {
                            var key = !await reader.IsDBNullAsync("state") ? (GalleryItemState)reader.GetInt32("state") : GalleryItemState.Pending;
                            var value = !await reader.IsDBNullAsync("count") ? reader.GetInt64("count") : 0;
                            results.Add(key.ToString(), value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to parse gallery item model from database - {ex?.Message}");
                        }
                    }
                }

                await conn.CloseAsync();
            }

            foreach (var s in Enum.GetNames(typeof(GalleryItemState)))
            {
                if (!results.ContainsKey(s))
                {
                    results.Add(s, s.Equals("All", StringComparison.OrdinalIgnoreCase) ? results.Sum(x => x.Value) : 0);
                }
            }

            return results;
        }

        public async Task<List<GalleryItemModel>> GetAllGalleryItems(int? galleryId, GalleryItemState state = GalleryItemState.All, MediaType type = MediaType.All, ImageOrientation orientation = ImageOrientation.None, GalleryGroup group = GalleryGroup.None, GalleryOrder order = GalleryOrder.Descending, int limit = int.MaxValue, int page = 1)
        {
            List<GalleryItemModel> result;

            using (var conn = await GetConnection())
            {
                var query = $"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE {(galleryId != null && galleryId > 0 ? "gi.`gallery_id`=@Id" : "gi.`gallery_id` > 0")}{(type != MediaType.All ? " AND gi.`media_type`=@Type" : string.Empty)}{(orientation != ImageOrientation.None ? " AND gi.`orientation`=@Orientation" : string.Empty)}{(state != GalleryItemState.All ? " AND gi.`state`=@State" : string.Empty)};";
                switch (group)
                {
                    case GalleryGroup.Date:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`uploaded_date` {(order == GalleryOrder.Ascending ? "ASC" : "DESC")};";
                        break;
                    case GalleryGroup.Uploader:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`uploaded_by` {(order == GalleryOrder.Ascending ? "ASC" : "DESC")};";
                        break;
                    case GalleryGroup.MediaType:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`media_type` {(order == GalleryOrder.Ascending ? "ASC" : "DESC")};";
                        break;
                    case GalleryGroup.None:
                        switch (order)
                        {
                            case GalleryOrder.Random:
                                query = $"{query.TrimEnd(' ', ';')} ORDER BY RANDOM();";
                                break;
                            default:
                                query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`uploaded_date` {(order == GalleryOrder.Ascending ? "ASC" : "DESC")};";
                                break;
                        }
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
                cmd.Parameters.AddWithValue("Orientation", orientation);
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
                    var cmd = CreateCommand($"INSERT INTO `gallery_items` (`gallery_id`, `title`, `state`, `uploaded_by`, `uploaded_date`, `checksum`, `media_type`, `orientation`) VALUES (@GalleryId, @Title, @State, @UploadedBy, @UploadedDate, @Checksum, @MediaType, @Orientation); SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE gi.`id`=last_insert_rowid();", conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("GalleryId", model.GalleryId);
                    cmd.Parameters.AddWithValue("Title", model.Title);
                    cmd.Parameters.AddWithValue("State", (int)model.State);
                    cmd.Parameters.AddWithValue("UploadedBy", !string.IsNullOrWhiteSpace(model.UploadedBy) ? model.UploadedBy : DBNull.Value);
                    cmd.Parameters.AddWithValue("UploadedDate", ((model.UploadedDate ?? DateTime.UtcNow) - new DateTime(1970, 1, 1)).TotalSeconds);
                    cmd.Parameters.AddWithValue("Checksum", !string.IsNullOrWhiteSpace(model.Checksum) ? model.Checksum : DBNull.Value);
                    cmd.Parameters.AddWithValue("MediaType", (int)model.MediaType);
                    cmd.Parameters.AddWithValue("Orientation", (int)model.Orientation);

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
                var cmd = CreateCommand($"UPDATE `gallery_items` SET `title`=@Title, `state`=@State, `uploaded_by`=@UploadedBy, `uploaded_date`=@UploadedDate, `checksum`=@Checksum, `media_type`=@MediaType, `orientation`=@Orientation WHERE `id`=@Id; SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE gi.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Title", model.Title);
                cmd.Parameters.AddWithValue("State", (int)model.State);
                cmd.Parameters.AddWithValue("UploadedBy", !string.IsNullOrWhiteSpace(model.UploadedBy) ? model.UploadedBy : DBNull.Value);
                cmd.Parameters.AddWithValue("UploadedDate", model.UploadedDate != null ? ((DateTime)model.UploadedDate - new DateTime(1970, 1, 1)).TotalSeconds : (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
                cmd.Parameters.AddWithValue("Checksum", !string.IsNullOrWhiteSpace(model.Checksum) ? model.Checksum : DBNull.Value);
                cmd.Parameters.AddWithValue("MediaType", (int)model.MediaType);
                cmd.Parameters.AddWithValue("Orientation", (int)model.Orientation);

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
                var cmd = CreateCommand($"INSERT INTO `users` (`username`, `email`, `password`) VALUES (@Username, @Email, @Password); SELECT * FROM `users` WHERE `id`=last_insert_rowid();", conn);
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
            bool result = false;

            try
            {
                using (var backup = await GetConnection(path))
                using (var conn = await GetConnection())
                {
                    await backup.OpenAsync();
                    await conn.OpenAsync();

                    backup.BackupDatabase(conn);

                    await conn.CloseAsync();
                    await backup.CloseAsync();

                    ClearPool(backup);
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
                using (var conn = await GetConnection())
                using (var backup = await GetConnection(path))
                {
                    await conn.OpenAsync();
                    await backup.OpenAsync();

                    conn.BackupDatabase(backup);

                    await backup.CloseAsync();
                    await conn.CloseAsync();
                
                    ClearPool(backup);
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
                                GalleryName = !await reader.IsDBNullAsync("gallery_name") ? reader.GetString("gallery_name") : string.Empty,
                                Title = !await reader.IsDBNullAsync("title") ? reader.GetString("title") : string.Empty,
                                UploadedBy = !await reader.IsDBNullAsync("uploaded_by") ? reader.GetString("uploaded_by") : null,
                                UploadedDate = !await reader.IsDBNullAsync("uploaded_date") ? DateTime.UnixEpoch.AddSeconds(reader.GetInt32("uploaded_date")) : null,
                                Checksum = !await reader.IsDBNullAsync("checksum") ? reader.GetString("checksum") : null,
                                MediaType = !await reader.IsDBNullAsync("media_type") ? (MediaType)reader.GetInt32("media_type") : MediaType.Unknown,
                                Orientation = !await reader.IsDBNullAsync("orientation") ? (ImageOrientation)reader.GetInt32("orientation") : ImageOrientation.None,
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

        private async Task<List<GalleryItemModel>> ReadPendingGalleryItems(SqliteDataReader? reader)
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
                                Orientation = !await reader.IsDBNullAsync("orientation") ? (ImageOrientation)reader.GetInt32("orientation") : ImageOrientation.None,
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
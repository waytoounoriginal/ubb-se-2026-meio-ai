using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    public class ReelRepository
    {
        private readonly ISqlConnectionFactory _db;
        public ReelRepository(ISqlConnectionFactory db) => _db = db;

        // Returns all reels where CreatorUserId = userId
        public async Task<IList<ReelModel>> GetUserReelsAsync(int userId)
        {
            const string sql = @"
                SELECT ReelId, MovieId, CreatorUserId, VideoUrl, ThumbnailUrl,
                       Title, Caption, FeatureDurationSeconds, BackgroundMusicId,
                       CropDataJson, Source, CreatedAt, LastEditedAt
                FROM Reel
                WHERE CreatorUserId = @UserId
                ORDER BY CreatedAt DESC";
            var result = new List<ReelModel>();
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ReelModel
                {
                    ReelId = reader.GetInt32(0),
                    MovieId = reader.GetInt32(1),
                    CreatorUserId = reader.GetInt32(2),
                    VideoUrl = reader.GetString(3),
                    ThumbnailUrl = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Title = reader.GetString(5),
                    Caption = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    FeatureDurationSeconds = reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
                    BackgroundMusicId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    CropDataJson = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Source = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    CreatedAt = reader.GetDateTime(11),
                    LastEditedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                });
            }
            return result;
        }

        // Updates CropDataJson, BackgroundMusicId, LastEditedAt for a reel.
        // Returns the number of rows affected (should be 1).
        public async Task<int> UpdateReelEditsAsync(int reelId, string cropDataJson, int? musicId)
        {
            const string sql = @"
                UPDATE Reel
                SET CropDataJson = @Crop,
                    BackgroundMusicId = @MusicId,
                    LastEditedAt = SYSUTCDATETIME()
                WHERE ReelId = @ReelId";
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Crop", cropDataJson);
            cmd.Parameters.AddWithValue("@MusicId", (object?)musicId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReelId", reelId);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<ReelModel?> GetReelByIdAsync(int reelId)
        {
            const string sql = @"
                SELECT ReelId, MovieId, CreatorUserId, VideoUrl, ThumbnailUrl,
                       Title, Caption, FeatureDurationSeconds, BackgroundMusicId,
                       CropDataJson, Source, CreatedAt, LastEditedAt
                FROM Reel
                WHERE ReelId = @ReelId";

            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ReelId", reelId);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new ReelModel
            {
                ReelId = reader.GetInt32(0),
                MovieId = reader.GetInt32(1),
                CreatorUserId = reader.GetInt32(2),
                VideoUrl = reader.GetString(3),
                ThumbnailUrl = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Title = reader.GetString(5),
                Caption = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                FeatureDurationSeconds = reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
                BackgroundMusicId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                CropDataJson = reader.IsDBNull(9) ? null : reader.GetString(9),
                Source = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                CreatedAt = reader.GetDateTime(11),
                LastEditedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
            };
        }

        // Deletes a reel from the database
        public async Task DeleteReelAsync(int reelId)
        {
            const string sql = @"
                DELETE FROM UserReelInteraction WHERE ReelId = @ReelId;
                DELETE FROM Reel WHERE ReelId = @ReelId;";
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ReelId", reelId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Persists user–reel interactions (likes, views) to the UserReelInteraction table.
    /// Owner: Tudor
    /// </summary>
    public class ReelInteractionService : IReelInteractionService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ReelInteractionService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Toggles the IsLiked flag for a (UserId, ReelId) pair.
        /// If no interaction row exists yet, inserts one with IsLiked = 1.
        /// If a row exists, flips IsLiked.
        /// </summary>
        public async Task ToggleLikeAsync(int userId, int reelId)
        {
            const string sql = @"
                IF EXISTS (SELECT 1 FROM UserReelInteraction WHERE UserId = @UserId AND ReelId = @ReelId)
                BEGIN
                    UPDATE UserReelInteraction
                    SET IsLiked = CASE WHEN IsLiked = 1 THEN 0 ELSE 1 END
                    WHERE UserId = @UserId AND ReelId = @ReelId;
                END
                ELSE
                BEGIN
                    INSERT INTO UserReelInteraction (UserId, ReelId, IsLiked, WatchDurationSec, WatchPercentage, ViewedAt)
                    VALUES (@UserId, @ReelId, 1, 0, 0, SYSUTCDATETIME());
                END
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ReelId", reelId);
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Records a view interaction. Upserts the row — on repeat views, updates watch data.
        /// </summary>
        public async Task RecordViewAsync(int userId, int reelId, double watchDurationSec, double watchPercentage)
        {
            const string sql = @"
                IF EXISTS (SELECT 1 FROM UserReelInteraction WHERE UserId = @UserId AND ReelId = @ReelId)
                BEGIN
                    UPDATE UserReelInteraction
                    SET WatchDurationSec = @WatchDurationSec,
                        WatchPercentage  = @WatchPercentage,
                        ViewedAt         = SYSUTCDATETIME()
                    WHERE UserId = @UserId AND ReelId = @ReelId;
                END
                ELSE
                BEGIN
                    INSERT INTO UserReelInteraction (UserId, ReelId, IsLiked, WatchDurationSec, WatchPercentage, ViewedAt)
                    VALUES (@UserId, @ReelId, 0, @WatchDurationSec, @WatchPercentage, SYSUTCDATETIME());
                END
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ReelId", reelId);
            command.Parameters.AddWithValue("@WatchDurationSec", watchDurationSec);
            command.Parameters.AddWithValue("@WatchPercentage", watchPercentage);
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Returns the interaction record for a user–reel pair, or null if none exists.
        /// </summary>
        public async Task<UserReelInteractionModel?> GetInteractionAsync(int userId, int reelId)
        {
            const string sql = @"
                SELECT InteractionId, UserId, ReelId, IsLiked, WatchDurationSec, WatchPercentage, ViewedAt
                FROM UserReelInteraction
                WHERE UserId = @UserId AND ReelId = @ReelId
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ReelId", reelId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserReelInteractionModel
                {
                    InteractionId = reader.GetInt64(0),
                    UserId = reader.GetInt32(1),
                    ReelId = reader.GetInt32(2),
                    IsLiked = reader.GetBoolean(3),
                    WatchDurationSec = reader.GetDouble(4),
                    WatchPercentage = reader.GetDouble(5),
                    ViewedAt = reader.GetDateTime(6)
                };
            }

            return null;
        }
    }
}

using System;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Raw SQL data access for the UserReelInteraction table.
    /// Owner: Tudor
    /// </summary>
    public class InteractionRepository : IInteractionRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public InteractionRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InsertInteractionAsync(UserReelInteractionModel interaction)
        {
            const string sql = @"
                INSERT INTO UserReelInteraction
                    (UserId, ReelId, IsLiked, WatchDurationSec, WatchPercentage, ViewedAt)
                VALUES
                    (@UserId, @ReelId, @IsLiked, @WatchDurationSec, @WatchPercentage, SYSUTCDATETIME())
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", interaction.UserId);
            command.Parameters.AddWithValue("@ReelId", interaction.ReelId);
            command.Parameters.AddWithValue("@IsLiked", interaction.IsLiked);
            command.Parameters.AddWithValue("@WatchDurationSec", interaction.WatchDurationSec);
            command.Parameters.AddWithValue("@WatchPercentage", interaction.WatchPercentage);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpsertInteractionAsync(int userId, int reelId)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM UserReelInteraction WHERE UserId = @UserId AND ReelId = @ReelId)
                BEGIN
                    INSERT INTO UserReelInteraction (UserId, ReelId, IsLiked, WatchDurationSec, WatchPercentage, ViewedAt)
                    VALUES (@UserId, @ReelId, 0, 0, 0, SYSUTCDATETIME());
                END
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ReelId", reelId);
            await command.ExecuteNonQueryAsync();
        }

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

        public async Task UpdateViewDataAsync(int userId, int reelId, double watchDurationSec, double watchPercentage)
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

        public async Task<int> GetLikeCountAsync(int reelId)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM UserReelInteraction
                WHERE ReelId = @ReelId AND IsLiked = 1
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ReelId", reelId);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int?> GetReelMovieIdAsync(int reelId)
        {
            const string sql = "SELECT MovieId FROM Reel WHERE ReelId = @ReelId";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ReelId", reelId);
            var result = await command.ExecuteScalarAsync();
            return result == null ? null : Convert.ToInt32(result);
        }
    }
}

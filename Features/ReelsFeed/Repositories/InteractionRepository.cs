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

        // Column indices for UserReelInteractionModel mapping
        private const int UserReelInteractionModel_InteractionId_Index = 0;
        private const int UserReelInteractionModel_UserId_Index = 1;
        private const int UserReelInteractionModel_ReelId_Index = 2;
        private const int UserReelInteractionModel_IsLiked_Index = 3;
        private const int UserReelInteractionModel_WatchDurationSec_Index = 4;
        private const int UserReelInteractionModel_WatchPercentage_Index = 5;
        private const int UserReelInteractionModel_ViewedAt_Index = 6;

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
            var interaction = await GetInteractionAsync(userId, reelId);
            
            if (interaction == null)
            {
                var newInteraction = new UserReelInteractionModel
                {
                    UserId = userId,
                    ReelId = reelId,
                    IsLiked = true,
                    WatchDurationSec = 0,
                    WatchPercentage = 0,
                    ViewedAt = DateTime.UtcNow
                };
                await InsertInteractionAsync(newInteraction);
                return;
            }

            const string sql = @"
                UPDATE UserReelInteraction
                SET IsLiked = CASE WHEN IsLiked = 1 THEN 0 ELSE 1 END
                WHERE UserId = @UserId AND ReelId = @ReelId
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ReelId", reelId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateViewDataAsync(int userId, int reelId, double watchDurationSec, double watchPercentage)
        {
            var interaction = await GetInteractionAsync(userId, reelId);
            
            if (interaction == null)
            {
                var newInteraction = new UserReelInteractionModel
                {
                    UserId = userId,
                    ReelId = reelId,
                    IsLiked = false,
                    WatchDurationSec = watchDurationSec,
                    WatchPercentage = watchPercentage,
                    ViewedAt = DateTime.UtcNow
                };
                await InsertInteractionAsync(newInteraction);
                return;
            }

            const string sql = @"
                UPDATE UserReelInteraction
                SET WatchDurationSec = @WatchDurationSec,
                    WatchPercentage  = @WatchPercentage,
                    ViewedAt         = SYSUTCDATETIME()
                WHERE UserId = @UserId AND ReelId = @ReelId
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
                return MapUserReelInteraction(reader);
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

        private UserReelInteractionModel MapUserReelInteraction(SqlDataReader reader)
        {
            return new UserReelInteractionModel
            {
                InteractionId = reader.GetInt64(UserReelInteractionModel_InteractionId_Index),
                UserId = reader.GetInt32(UserReelInteractionModel_UserId_Index),
                ReelId = reader.GetInt32(UserReelInteractionModel_ReelId_Index),
                IsLiked = reader.GetBoolean(UserReelInteractionModel_IsLiked_Index),
                WatchDurationSec = reader.GetDouble(UserReelInteractionModel_WatchDurationSec_Index),
                WatchPercentage = reader.GetDouble(UserReelInteractionModel_WatchPercentage_Index),
                ViewedAt = reader.GetDateTime(UserReelInteractionModel_ViewedAt_Index)
            };
        }
    }
}

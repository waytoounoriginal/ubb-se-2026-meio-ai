using System;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">Factory used to create SQL connections.</param>
        public InteractionRepository(ISqlConnectionFactory connectionFactory)
        {
            this._connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        public async Task InsertInteractionAsync(UserReelInteractionModel interaction)
        {
            const string insertInteractionSql = @"
                INSERT INTO UserReelInteraction
                    (UserId, ReelId, IsLiked, WatchDurationSec, WatchPercentage, ViewedAt)
                VALUES
                    (@UserId, @ReelId, @IsLiked, @WatchDurationSec, @WatchPercentage, SYSUTCDATETIME())
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var insertCommand = new SqlCommand(insertInteractionSql, connection);
            insertCommand.Parameters.AddWithValue("@UserId", interaction.UserId);
            insertCommand.Parameters.AddWithValue("@ReelId", interaction.ReelId);
            insertCommand.Parameters.AddWithValue("@IsLiked", interaction.IsLiked);
            insertCommand.Parameters.AddWithValue("@WatchDurationSec", interaction.WatchDurationSec);
            insertCommand.Parameters.AddWithValue("@WatchPercentage", interaction.WatchPercentage);
            await insertCommand.ExecuteNonQueryAsync();
        }

        /// <inheritdoc />
        public async Task UpsertInteractionAsync(int userId, int reelId)
        {
            const string upsertInteractionSql = @"
                IF NOT EXISTS (SELECT 1 FROM UserReelInteraction WHERE UserId = @UserId AND ReelId = @ReelId)
                BEGIN
                    INSERT INTO UserReelInteraction (UserId, ReelId, IsLiked, WatchDurationSec, WatchPercentage, ViewedAt)
                    VALUES (@UserId, @ReelId, 0, 0, 0, SYSUTCDATETIME());
                END
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var upsertCommand = new SqlCommand(upsertInteractionSql, connection);
            upsertCommand.Parameters.AddWithValue("@UserId", userId);
            upsertCommand.Parameters.AddWithValue("@ReelId", reelId);
            await upsertCommand.ExecuteNonQueryAsync();
        }

        /// <inheritdoc />
        public async Task ToggleLikeAsync(int userId, int reelId)
        {
            var existingInteraction = await this.GetInteractionAsync(userId, reelId);
            
            if (existingInteraction == null)
            {
                var interactionToInsert = new UserReelInteractionModel
                {
                    UserId = userId,
                    ReelId = reelId,
                    IsLiked = true,
                    WatchDurationSec = 0,
                    WatchPercentage = 0,
                    ViewedAt = DateTime.UtcNow
                };
                await this.InsertInteractionAsync(interactionToInsert);
                return;
            }

            const string toggleLikeSql = @"
                UPDATE UserReelInteraction
                SET IsLiked = CASE WHEN IsLiked = 1 THEN 0 ELSE 1 END
                WHERE UserId = @UserId AND ReelId = @ReelId
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var toggleLikeCommand = new SqlCommand(toggleLikeSql, connection);
            toggleLikeCommand.Parameters.AddWithValue("@UserId", userId);
            toggleLikeCommand.Parameters.AddWithValue("@ReelId", reelId);
            await toggleLikeCommand.ExecuteNonQueryAsync();
        }

        /// <inheritdoc />
        public async Task UpdateViewDataAsync(int userId, int reelId, double watchDurationSec, double watchPercentage)
        {
            var existingInteraction = await this.GetInteractionAsync(userId, reelId);
            
            if (existingInteraction == null)
            {
                var interactionToInsert = new UserReelInteractionModel
                {
                    UserId = userId,
                    ReelId = reelId,
                    IsLiked = false,
                    WatchDurationSec = watchDurationSec,
                    WatchPercentage = watchPercentage,
                    ViewedAt = DateTime.UtcNow,
                };
                await this.InsertInteractionAsync(interactionToInsert);
                return;
            }

            const string updateViewDataSql = @"
                UPDATE UserReelInteraction
                SET WatchDurationSec = @WatchDurationSec,
                    WatchPercentage  = @WatchPercentage,
                    ViewedAt         = SYSUTCDATETIME()
                WHERE UserId = @UserId AND ReelId = @ReelId
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var updateViewDataCommand = new SqlCommand(updateViewDataSql, connection);
            updateViewDataCommand.Parameters.AddWithValue("@UserId", userId);
            updateViewDataCommand.Parameters.AddWithValue("@ReelId", reelId);
            updateViewDataCommand.Parameters.AddWithValue("@WatchDurationSec", watchDurationSec);
            updateViewDataCommand.Parameters.AddWithValue("@WatchPercentage", watchPercentage);
            await updateViewDataCommand.ExecuteNonQueryAsync();
        }

        /// <inheritdoc />
        public async Task<UserReelInteractionModel?> GetInteractionAsync(int userId, int reelId)
        {
            const string getInteractionSql = @"
                SELECT InteractionId, UserId, ReelId, IsLiked, WatchDurationSec, WatchPercentage, ViewedAt
                FROM UserReelInteraction
                WHERE UserId = @UserId AND ReelId = @ReelId
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var getInteractionCommand = new SqlCommand(getInteractionSql, connection);
            getInteractionCommand.Parameters.AddWithValue("@UserId", userId);
            getInteractionCommand.Parameters.AddWithValue("@ReelId", reelId);

            await using var dataReader = await getInteractionCommand.ExecuteReaderAsync();
            if (await dataReader.ReadAsync())
            {
                return this.MapUserReelInteraction(dataReader);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<int> GetLikeCountAsync(int reelId)
        {
            const string getLikeCountSql = @"
                SELECT COUNT(*)
                FROM UserReelInteraction
                WHERE ReelId = @ReelId AND IsLiked = 1
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var getLikeCountCommand = new SqlCommand(getLikeCountSql, connection);
            getLikeCountCommand.Parameters.AddWithValue("@ReelId", reelId);
            var likeCountResult = await getLikeCountCommand.ExecuteScalarAsync();
            return Convert.ToInt32(likeCountResult);
        }

        /// <inheritdoc />
        public async Task<int?> GetReelMovieIdAsync(int reelId)
        {
            const string getReelMovieIdSql = "SELECT MovieId FROM Reel WHERE ReelId = @ReelId";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var getReelMovieIdCommand = new SqlCommand(getReelMovieIdSql, connection);
            getReelMovieIdCommand.Parameters.AddWithValue("@ReelId", reelId);
            var movieIdResult = await getReelMovieIdCommand.ExecuteScalarAsync();
            return movieIdResult == null ? null : Convert.ToInt32(movieIdResult);
        }

        private UserReelInteractionModel MapUserReelInteraction(SqlDataReader reader)
        {
            return new UserReelInteractionModel
            {
                InteractionId = reader.GetInt64(DataReaderColumnIndexes.UserReelInteractionModel.InteractionId),
                UserId = reader.GetInt32(DataReaderColumnIndexes.UserReelInteractionModel.UserId),
                ReelId = reader.GetInt32(DataReaderColumnIndexes.UserReelInteractionModel.ReelId),
                IsLiked = reader.GetBoolean(DataReaderColumnIndexes.UserReelInteractionModel.IsLiked),
                WatchDurationSec = reader.GetDouble(DataReaderColumnIndexes.UserReelInteractionModel.WatchDurationSec),
                WatchPercentage = reader.GetDouble(DataReaderColumnIndexes.UserReelInteractionModel.WatchPercentage),
                ViewedAt = reader.GetDateTime(DataReaderColumnIndexes.UserReelInteractionModel.ViewedAt),
            };
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Provides personalized reel recommendations by scoring unwatched reels
    /// against the user's movie preferences, with a cold-start fallback for new users.
    /// Owner: Tudor
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public RecommendationService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Returns the top <paramref name="count"/> recommended reels for the user.
        ///
        /// Algorithm (per docs Task 6):
        /// 1. Check if the user has any UserMoviePreference rows (indicates engagement history).
        /// 2a. If YES (warm user): query Reel LEFT JOIN UserMoviePreference on MovieId,
        ///     excluding already-viewed reels, and order by preference score descending
        ///     (highest-affinity movies first), with recency as tiebreaker.
        /// 2b. If NO (cold-start): serve globally popular reels — most-liked in the last 7 days,
        ///     excluding already-viewed, with recency as tiebreaker.
        /// </summary>
        public async Task<IList<ReelModel>> GetRecommendedReelsAsync(int userId, int count)
        {
            bool hasPreferences = await UserHasPreferencesAsync(userId);

            return hasPreferences
                ? await GetPersonalizedReelsAsync(userId, count)
                : await GetColdStartReelsAsync(userId, count);
        }

        /// <summary>
        /// Returns true if the user has at least one UserMoviePreference row,
        /// indicating they have engaged with the system (swipes, tournament, reel likes).
        /// </summary>
        private async Task<bool> UserHasPreferencesAsync(int userId)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM UserMoviePreference WHERE UserId = @UserId
                ) THEN 1 ELSE 0 END
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) == 1;
        }

        /// <summary>
        /// Warm-user path: scores unwatched reels by matching their MovieId against
        /// the user's UserMoviePreference scores. Higher-preference movies surface first.
        /// Reels whose MovieId has no preference row get a neutral score of 0.
        /// Recency (CreatedAt DESC) breaks ties.
        /// </summary>
        private async Task<IList<ReelModel>> GetPersonalizedReelsAsync(int userId, int count)
        {
            const string sql = @"
                SELECT TOP (@Count)
                    r.ReelId, r.MovieId, r.CreatorUserId, r.VideoUrl, r.ThumbnailUrl,
                    r.Title, r.Caption, r.FeatureDurationSeconds, r.CropDataJson,
                    r.BackgroundMusicId, r.Source, r.CreatedAt, r.LastEditedAt
                FROM Reel r
                LEFT JOIN UserMoviePreference p
                    ON p.MovieId = r.MovieId AND p.UserId = @UserId
                WHERE r.ReelId NOT IN (
                    SELECT ReelId FROM UserReelInteraction WHERE UserId = @UserId
                )
                ORDER BY
                    ISNULL(p.Score, 0) DESC,
                    r.CreatedAt DESC
            ";

            return await ExecuteReelQueryAsync(sql, cmd =>
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Count", count);
            });
        }

        /// <summary>
        /// Cold-start path for new users with no preference data.
        /// Serves globally trending reels: most-liked in the last 7 days,
        /// excluding any already-viewed by this user.
        /// Falls back to most recent reels if nothing was liked recently.
        /// </summary>
        private async Task<IList<ReelModel>> GetColdStartReelsAsync(int userId, int count)
        {
            const string sql = @"
                SELECT TOP (@Count)
                    r.ReelId, r.MovieId, r.CreatorUserId, r.VideoUrl, r.ThumbnailUrl,
                    r.Title, r.Caption, r.FeatureDurationSeconds, r.CropDataJson,
                    r.BackgroundMusicId, r.Source, r.CreatedAt, r.LastEditedAt
                FROM Reel r
                LEFT JOIN (
                    SELECT ReelId, COUNT(*) AS LikeCount
                    FROM UserReelInteraction
                    WHERE IsLiked = 1 AND ViewedAt >= DATEADD(DAY, -7, SYSUTCDATETIME())
                    GROUP BY ReelId
                ) likes ON likes.ReelId = r.ReelId
                WHERE r.ReelId NOT IN (
                    SELECT ReelId FROM UserReelInteraction WHERE UserId = @UserId
                )
                ORDER BY
                    ISNULL(likes.LikeCount, 0) DESC,
                    r.CreatedAt DESC
            ";

            return await ExecuteReelQueryAsync(sql, cmd =>
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Count", count);
            });
        }

        /// <summary>
        /// Shared helper that executes a reel query and maps the result set to ReelModel objects.
        /// </summary>
        private async Task<IList<ReelModel>> ExecuteReelQueryAsync(
            string sql, Action<SqlCommand> configureParameters)
        {
            var reels = new List<ReelModel>();

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            configureParameters(command);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                reels.Add(new ReelModel
                {
                    ReelId = reader.GetInt32(0),
                    MovieId = reader.GetInt32(1),
                    CreatorUserId = reader.GetInt32(2),
                    VideoUrl = reader.GetString(3),
                    ThumbnailUrl = reader.GetString(4),
                    Title = reader.GetString(5),
                    Caption = reader.GetString(6),
                    FeatureDurationSeconds = reader.GetDouble(7),
                    CropDataJson = reader.IsDBNull(8) ? null : reader.GetString(8),
                    BackgroundMusicId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    Source = reader.GetString(10),
                    CreatedAt = reader.GetDateTime(11),
                    LastEditedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12)
                });
            }

            return reels;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Provides personalized reel recommendations by scoring unwatched reels
    /// against the user's movie preferences, with a cold-start fallback for new users.
    /// Owner: Tudor.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public RecommendationService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        public async Task<IList<ReelModel>> GetRecommendedReelsAsync(int userId, int count)
        {
            // Choose recommendation strategy based on whether user preference history exists.
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
                SELECT TOP 1 1
                FROM UserMoviePreference
                WHERE UserId = @UserId
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }

        /// <summary>
        /// Warm-user path: ranks reels by user movie preference score in C#,
        /// using recency as a tiebreaker.
        /// </summary>
        private async Task<IList<ReelModel>> GetPersonalizedReelsAsync(int userId, int count)
        {
            var reels = await GetAllReelsAsync();
            var preferenceScores = await GetUserPreferenceScoresAsync(userId);

            return reels
                .OrderByDescending(reel =>
                    preferenceScores.TryGetValue(reel.MovieId, out var score) ? score : 0)
                .ThenByDescending(reel => reel.CreatedAt)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Cold-start path: ranks reels by recent like count in C#,
        /// using recency as a tiebreaker.
        /// </summary>
        private async Task<IList<ReelModel>> GetColdStartReelsAsync(int userId, int count)
        {
            var reels = await GetAllReelsAsync();
            var recentLikeCounts = await GetRecentLikeCountsAsync();

            return reels
                .OrderByDescending(reel =>
                    recentLikeCounts.TryGetValue(reel.ReelId, out var likeCount) ? likeCount : 0)
                .ThenByDescending(reel => reel.CreatedAt)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Retrieves all reels with associated movie genre metadata.
        /// </summary>
        private async Task<IList<ReelModel>> GetAllReelsAsync()
        {
            const string sql = @"
                SELECT
                    r.ReelId, r.MovieId, r.CreatorUserId, r.VideoUrl, r.ThumbnailUrl,
                    r.Title, r.Caption, r.FeatureDurationSeconds, r.CropDataJson,
                    r.BackgroundMusicId, r.Source, r.CreatedAt, r.LastEditedAt,
                    m.PrimaryGenre
                FROM Reel r
                LEFT JOIN Movie m ON m.MovieId = r.MovieId
            ";

            var reels = new List<ReelModel>();

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

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
                    LastEditedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                    Genre = reader.IsDBNull(13) ? null : reader.GetString(13),
                });
            }

            return reels;
        }

        /// <summary>
        /// Retrieves movie preference scores for a user.
        /// </summary>
        private async Task<Dictionary<int, double>> GetUserPreferenceScoresAsync(int userId)
        {
            const string sql = @"
                SELECT MovieId, Score
                FROM UserMoviePreference
                WHERE UserId = @UserId
            ";

            var scores = new Dictionary<int, double>();

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                scores[reader.GetInt32(0)] = reader.GetDouble(1);
            }

            return scores;
        }

        /// <summary>
        /// Retrieves recent like counts (last 7 days) grouped by reel.
        /// </summary>
        private async Task<Dictionary<int, int>> GetRecentLikeCountsAsync()
        {
            const string sql = @"
                SELECT ReelId, COUNT(*) AS LikeCount
                FROM UserReelInteraction
                WHERE IsLiked = 1 AND ViewedAt >= DATEADD(DAY, -7, SYSUTCDATETIME())
                GROUP BY ReelId
            ";

            var likeCounts = new Dictionary<int, int>();

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                likeCounts[reader.GetInt32(0)] = reader.GetInt32(1);
            }

            return likeCounts;
        }
    }
}

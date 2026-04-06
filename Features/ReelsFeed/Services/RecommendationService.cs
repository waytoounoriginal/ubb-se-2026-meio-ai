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
        private const int ReelModel_ReelId_Index = 0;
        private const int ReelModel_MovieId_Index = 1;
        private const int ReelModel_CreatorUserId_Index = 2;
        private const int ReelModel_VideoUrl_Index = 3;
        private const int ReelModel_ThumbnailUrl_Index = 4;
        private const int ReelModel_Title_Index = 5;
        private const int ReelModel_Caption_Index = 6;
        private const int ReelModel_FeatureDurationSeconds_Index = 7;
        private const int ReelModel_CropDataJson_Index = 8;
        private const int ReelModel_BackgroundMusicId_Index = 9;
        private const int ReelModel_Source_Index = 10;
        private const int ReelModel_CreatedAt_Index = 11;
        private const int ReelModel_LastEditedAt_Index = 12;
        private const int ReelModel_PrimaryGenre_Index = 13;

        private const int UserMoviePreference_MovieId_Index = 0;
        private const int UserMoviePreference_Score_Index = 1;

        private const int UserReelInteraction_ReelId_Index = 0;
        private const int UserReelInteraction_LikeCount_Index = 1;

        private readonly ISqlConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecommendationService"/> class.
        /// </summary>
        /// <param name="connectionFactory">Factory used to create SQL connections.</param>
        public RecommendationService(ISqlConnectionFactory connectionFactory)
        {
            this._connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        public async Task<IList<ReelModel>> GetRecommendedReelsAsync(int userId, int count)
        {
            // Choose recommendation strategy based on whether user preference history exists.
            bool userHasPreferences = await this.UserHasPreferencesAsync(userId);

            return userHasPreferences
                ? await this.GetPersonalizedReelsAsync(userId, count)
                : await this.GetColdStartReelsAsync(userId, count);
        }

        /// <summary>
        /// Returns true if the user has at least one UserMoviePreference row,
        /// indicating they have engaged with the system (swipes, tournament, reel likes).
        /// </summary>
        private async Task<bool> UserHasPreferencesAsync(int userId)
        {
            const string checkUserHasPreferencesSql = @"
                SELECT TOP 1 1
                FROM UserMoviePreference
                WHERE UserId = @UserId
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var checkUserHasPreferencesCommand = new SqlCommand(checkUserHasPreferencesSql, connection);
            checkUserHasPreferencesCommand.Parameters.AddWithValue("@UserId", userId);
            var hasPreferencesResult = await checkUserHasPreferencesCommand.ExecuteScalarAsync();
            return hasPreferencesResult != null;
        }

        /// <summary>
        /// Warm-user path: ranks reels by user movie preference score in C#,
        /// using recency as a tiebreaker.
        /// </summary>
        private async Task<IList<ReelModel>> GetPersonalizedReelsAsync(int userId, int count)
        {
            var allReels = await this.GetAllReelsAsync();
            var userPreferenceScores = await this.GetUserPreferenceScoresAsync(userId);

            return allReels
                .OrderByDescending(reel =>
                    userPreferenceScores.TryGetValue(reel.MovieId, out var preferenceScore) ? preferenceScore : 0)
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
            var allReels = await this.GetAllReelsAsync();
            var recentLikeCountsByReelId = await this.GetRecentLikeCountsAsync();

            return allReels
                .OrderByDescending(reel =>
                    recentLikeCountsByReelId.TryGetValue(reel.ReelId, out var recentLikeCount) ? recentLikeCount : 0)
                .ThenByDescending(reel => reel.CreatedAt)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Retrieves all reels with associated movie genre metadata.
        /// </summary>
        private async Task<IList<ReelModel>> GetAllReelsAsync()
        {
            const string getAllReelsSql = @"
                SELECT
                    r.ReelId, r.MovieId, r.CreatorUserId, r.VideoUrl, r.ThumbnailUrl,
                    r.Title, r.Caption, r.FeatureDurationSeconds, r.CropDataJson,
                    r.BackgroundMusicId, r.Source, r.CreatedAt, r.LastEditedAt,
                    m.PrimaryGenre
                FROM Reel r
                LEFT JOIN Movie m ON m.MovieId = r.MovieId
            ";

            var allReels = new List<ReelModel>();

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var getAllReelsCommand = new SqlCommand(getAllReelsSql, connection);

            await using var reelReader = await getAllReelsCommand.ExecuteReaderAsync();
            while (await reelReader.ReadAsync())
            {
                allReels.Add(this.MapReel(reelReader));
            }

            return allReels;
        }

        /// <summary>
        /// Retrieves movie preference scores for a user.
        /// </summary>
        private async Task<Dictionary<int, double>> GetUserPreferenceScoresAsync(int userId)
        {
            const string getUserPreferenceScoresSql = @"
                SELECT MovieId, Score
                FROM UserMoviePreference
                WHERE UserId = @UserId
            ";

            var preferenceScoresByMovieId = new Dictionary<int, double>();

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var getUserPreferenceScoresCommand = new SqlCommand(getUserPreferenceScoresSql, connection);
            getUserPreferenceScoresCommand.Parameters.AddWithValue("@UserId", userId);

            await using var preferenceScoreReader = await getUserPreferenceScoresCommand.ExecuteReaderAsync();
            while (await preferenceScoreReader.ReadAsync())
            {
                preferenceScoresByMovieId[preferenceScoreReader.GetInt32(UserMoviePreference_MovieId_Index)] =
                    preferenceScoreReader.GetDouble(UserMoviePreference_Score_Index);
            }

            return preferenceScoresByMovieId;
        }

        /// <summary>
        /// Retrieves recent like counts (last 7 days) grouped by reel.
        /// </summary>
        private async Task<Dictionary<int, int>> GetRecentLikeCountsAsync()
        {
            const string getRecentLikeCountsSql = @"
                SELECT ReelId, COUNT(*) AS LikeCount
                FROM UserReelInteraction
                WHERE IsLiked = 1 AND ViewedAt >= DATEADD(DAY, -7, SYSUTCDATETIME())
                GROUP BY ReelId
            ";

            var recentLikeCountsByReelId = new Dictionary<int, int>();

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var getRecentLikeCountsCommand = new SqlCommand(getRecentLikeCountsSql, connection);

            await using var recentLikeCountReader = await getRecentLikeCountsCommand.ExecuteReaderAsync();
            while (await recentLikeCountReader.ReadAsync())
            {
                recentLikeCountsByReelId[recentLikeCountReader.GetInt32(UserReelInteraction_ReelId_Index)] =
                    recentLikeCountReader.GetInt32(UserReelInteraction_LikeCount_Index);
            }

            return recentLikeCountsByReelId;
        }

        /// <summary>
        /// Maps a reel query result row to a <see cref="ReelModel"/>.
        /// </summary>
        /// <param name="reelReader">Reader positioned on a reel row.</param>
        /// <returns>The mapped reel model.</returns>
        private ReelModel MapReel(SqlDataReader reelReader)
        {
            return new ReelModel
            {
                ReelId = reelReader.GetInt32(ReelModel_ReelId_Index),
                MovieId = reelReader.GetInt32(ReelModel_MovieId_Index),
                CreatorUserId = reelReader.GetInt32(ReelModel_CreatorUserId_Index),
                VideoUrl = reelReader.GetString(ReelModel_VideoUrl_Index),
                ThumbnailUrl = reelReader.GetString(ReelModel_ThumbnailUrl_Index),
                Title = reelReader.GetString(ReelModel_Title_Index),
                Caption = reelReader.GetString(ReelModel_Caption_Index),
                FeatureDurationSeconds = reelReader.GetDouble(ReelModel_FeatureDurationSeconds_Index),
                CropDataJson = reelReader.IsDBNull(ReelModel_CropDataJson_Index) ? null : reelReader.GetString(ReelModel_CropDataJson_Index),
                BackgroundMusicId = reelReader.IsDBNull(ReelModel_BackgroundMusicId_Index) ? null : reelReader.GetInt32(ReelModel_BackgroundMusicId_Index),
                Source = reelReader.GetString(ReelModel_Source_Index),
                CreatedAt = reelReader.GetDateTime(ReelModel_CreatedAt_Index),
                LastEditedAt = reelReader.IsDBNull(ReelModel_LastEditedAt_Index) ? null : reelReader.GetDateTime(ReelModel_LastEditedAt_Index),
                Genre = reelReader.IsDBNull(ReelModel_PrimaryGenre_Index) ? null : reelReader.GetString(ReelModel_PrimaryGenre_Index),
            };
        }
    }
}

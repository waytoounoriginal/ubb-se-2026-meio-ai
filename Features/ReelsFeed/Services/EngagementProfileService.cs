using System;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Computes the user's engagement profile by aggregating raw interaction data,
    /// then delegates persistence to <see cref="IProfileRepository"/>.
    /// Owner: Tudor
    /// </summary>
    public class EngagementProfileService : IEngagementProfileService
    {
        private readonly IProfileRepository _profileRepository;
        private readonly ISqlConnectionFactory _connectionFactory;

        public EngagementProfileService(
            IProfileRepository profileRepository,
            ISqlConnectionFactory connectionFactory)
        {
            _profileRepository = profileRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<UserProfileModel?> GetProfileAsync(int userId)
        {
            return await _profileRepository.GetProfileAsync(userId);
        }

        /// <summary>
        /// Aggregates all UserReelInteraction rows for the given user, computes
        /// engagement stats, and persists the result via the profile repository.
        /// </summary>
        public async Task RefreshProfileAsync(int userId)
        {
            // Step 1: aggregate raw interaction data (read-only query)
            var profile = await AggregateInteractionStatsAsync(userId);

            // Step 2: persist via repository
            await _profileRepository.UpsertProfileAsync(profile);
        }

        /// <summary>
        /// Reads UserReelInteraction rows and computes engagement metrics.
        /// This is business logic (aggregation formulas), not a simple CRUD operation,
        /// so it lives in the service rather than the repository.
        /// </summary>
        private async Task<UserProfileModel> AggregateInteractionStatsAsync(int userId)
        {
            const string sql = @"
                SELECT
                    ISNULL(SUM(CASE WHEN IsLiked = 1 THEN 1 ELSE 0 END), 0),
                    ISNULL(CAST(SUM(WatchDurationSec) AS BIGINT), 0),
                    ISNULL(AVG(WatchDurationSec), 0),
                    COUNT(*)
                FROM UserReelInteraction
                WHERE UserId = @UserId
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();

            int totalLikes = reader.GetInt32(0);
            long totalWatchTimeSec = reader.GetInt64(1);
            double avgWatchTimeSec = reader.GetDouble(2);
            int totalClipsViewed = reader.GetInt32(3);
            double likeToViewRatio = totalClipsViewed > 0
                ? (double)totalLikes / totalClipsViewed
                : 0;

            return new UserProfileModel
            {
                UserId = userId,
                TotalLikes = totalLikes,
                TotalWatchTimeSec = totalWatchTimeSec,
                AvgWatchTimeSec = avgWatchTimeSec,
                TotalClipsViewed = totalClipsViewed,
                LikeToViewRatio = likeToViewRatio,
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}

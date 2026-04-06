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
    /// Owner: Tudor.
    /// </summary>
    public class EngagementProfileService : IEngagementProfileService
    {
        private const int EngagementStats_TotalLikes_Index = 0;
        private const int EngagementStats_TotalWatchTimeSec_Index = 1;
        private const int EngagementStats_AverageWatchTimeSec_Index = 2;
        private const int EngagementStats_TotalClipsViewed_Index = 3;

        private readonly IProfileRepository _profileRepository;
        private readonly ISqlConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="EngagementProfileService"/> class.
        /// </summary>
        /// <param name="profileRepository">Repository used for engagement profile persistence.</param>
        /// <param name="connectionFactory">Factory used to create SQL connections.</param>
        public EngagementProfileService(
            IProfileRepository profileRepository,
            ISqlConnectionFactory connectionFactory)
        {
            this._profileRepository = profileRepository;
            this._connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        public async Task<UserProfileModel?> GetProfileAsync(int userId)
        {
            return await this._profileRepository.GetProfileAsync(userId);
        }

        /// <inheritdoc />
        public async Task RefreshProfileAsync(int userId)
        {
            // Step 1: aggregate raw interaction data (read-only query)
            var refreshedProfile = await this.AggregateInteractionStatsAsync(userId);

            // Step 2: persist via repository
            await this._profileRepository.UpsertProfileAsync(refreshedProfile);
        }

        /// <summary>
        /// Reads UserReelInteraction rows and computes engagement metrics.
        /// This is business logic (aggregation formulas), not a simple CRUD operation,
        /// so it lives in the service rather than the repository.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The aggregated engagement profile.</returns>
        private async Task<UserProfileModel> AggregateInteractionStatsAsync(int userId)
        {
            const string aggregateInteractionStatsSql = @"
                SELECT
                    ISNULL(SUM(CASE WHEN IsLiked = 1 THEN 1 ELSE 0 END), 0),
                    ISNULL(CAST(SUM(WatchDurationSec) AS BIGINT), 0),
                    ISNULL(AVG(WatchDurationSec), 0),
                    COUNT(*)
                FROM UserReelInteraction
                WHERE UserId = @UserId
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var aggregateStatsCommand = new SqlCommand(aggregateInteractionStatsSql, connection);
            aggregateStatsCommand.Parameters.AddWithValue("@UserId", userId);

            await using var aggregateStatsReader = await aggregateStatsCommand.ExecuteReaderAsync();
            await aggregateStatsReader.ReadAsync();

            return this.MapAggregatedProfileStats(aggregateStatsReader, userId);
        }

        /// <summary>
        /// Maps aggregate query results to a <see cref="UserProfileModel"/>.
        /// </summary>
        /// <param name="aggregateStatsReader">Reader positioned on the aggregate result row.</param>
        /// <param name="userId">The ID of the user associated with the aggregated data.</param>
        /// <returns>The mapped engagement profile model.</returns>
        private UserProfileModel MapAggregatedProfileStats(SqlDataReader aggregateStatsReader, int userId)
        {
            int totalLikes = aggregateStatsReader.GetInt32(EngagementStats_TotalLikes_Index);
            long totalWatchTimeSec = aggregateStatsReader.GetInt64(EngagementStats_TotalWatchTimeSec_Index);
            double averageWatchTimeSec = aggregateStatsReader.GetDouble(EngagementStats_AverageWatchTimeSec_Index);
            int totalClipsViewed = aggregateStatsReader.GetInt32(EngagementStats_TotalClipsViewed_Index);
            double likeToViewRatio = totalClipsViewed > 0
                ? (double)totalLikes / totalClipsViewed
                : 0;

            return new UserProfileModel
            {
                UserId = userId,
                TotalLikes = totalLikes,
                TotalWatchTimeSec = totalWatchTimeSec,
                AvgWatchTimeSec = averageWatchTimeSec,
                TotalClipsViewed = totalClipsViewed,
                LikeToViewRatio = likeToViewRatio,
                LastUpdated = DateTime.UtcNow,
            };
        }
    }
}

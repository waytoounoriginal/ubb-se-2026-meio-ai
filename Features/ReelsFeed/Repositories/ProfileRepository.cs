using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{

    public class ProfileRepository : IProfileRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ProfileRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<UserProfileModel?> GetProfileAsync(int userId)
        {
            const string sql = @"
                SELECT UserProfileId, UserId, TotalLikes, TotalWatchTimeSec,
                       AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio, LastUpdated
                FROM UserProfile
                WHERE UserId = @UserId
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserProfileModel
                {
                    UserProfileId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    TotalLikes = reader.GetInt32(2),
                    TotalWatchTimeSec = reader.GetInt64(3),
                    AvgWatchTimeSec = reader.GetDouble(4),
                    TotalClipsViewed = reader.GetInt32(5),
                    LikeToViewRatio = reader.GetDouble(6),
                    LastUpdated = reader.GetDateTime(7)
                };
            }

            return null;
        }

        public async Task<UserProfileModel> BuildProfileFromInteractionsAsync(int userId)
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

        public async Task UpsertProfileAsync(UserProfileModel profile)
        {
            const string sql = @"
                IF EXISTS (SELECT 1 FROM UserProfile WHERE UserId = @UserId)
                BEGIN
                    UPDATE UserProfile
                    SET TotalLikes        = @TotalLikes,
                        TotalWatchTimeSec = @TotalWatchTimeSec,
                        AvgWatchTimeSec   = @AvgWatchTimeSec,
                        TotalClipsViewed  = @TotalClipsViewed,
                        LikeToViewRatio   = @LikeToViewRatio,
                        LastUpdated       = SYSUTCDATETIME()
                    WHERE UserId = @UserId;
                END
                ELSE
                BEGIN
                    INSERT INTO UserProfile
                        (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec,
                         TotalClipsViewed, LikeToViewRatio, LastUpdated)
                    VALUES
                        (@UserId, @TotalLikes, @TotalWatchTimeSec, @AvgWatchTimeSec,
                         @TotalClipsViewed, @LikeToViewRatio, SYSUTCDATETIME());
                END
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", profile.UserId);
            command.Parameters.AddWithValue("@TotalLikes", profile.TotalLikes);
            command.Parameters.AddWithValue("@TotalWatchTimeSec", profile.TotalWatchTimeSec);
            command.Parameters.AddWithValue("@AvgWatchTimeSec", profile.AvgWatchTimeSec);
            command.Parameters.AddWithValue("@TotalClipsViewed", profile.TotalClipsViewed);
            command.Parameters.AddWithValue("@LikeToViewRatio", profile.LikeToViewRatio);
            await command.ExecuteNonQueryAsync();
        }
    }
}

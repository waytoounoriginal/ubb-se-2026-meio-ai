using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{

    public class ProfileRepository : IProfileRepository
    {
        private const int UserProfileModel_UserProfileId_Index = 0;
        private const int UserProfileModel_UserId_Index = 1;
        private const int UserProfileModel_TotalLikes_Index = 2;
        private const int UserProfileModel_TotalWatchTimeSec_Index = 3;
        private const int UserProfileModel_AvgWatchTimeSec_Index = 4;
        private const int UserProfileModel_TotalClipsViewed_Index = 5;
        private const int UserProfileModel_LikeToViewRatio_Index = 6;
        private const int UserProfileModel_LastUpdated_Index = 7;

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
                return MapUserProfile(reader);
            }

            return null;
        }

        public async Task UpsertProfileAsync(UserProfileModel profile)
        {
            var exists = await ProfileExistsAsync(profile.UserId);

            if (!exists)
            {
                await InsertProfileAsync(profile);
                return;
            }

            await UpdateProfileAsync(profile);
        }

        private async Task<bool> ProfileExistsAsync(int userId)
        {
            const string sql = "SELECT 1 FROM UserProfile WHERE UserId = @UserId";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }

        private async Task InsertProfileAsync(UserProfileModel profile)
        {
            const string sql = @"
                INSERT INTO UserProfile
                    (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec,
                     TotalClipsViewed, LikeToViewRatio, LastUpdated)
                VALUES
                    (@UserId, @TotalLikes, @TotalWatchTimeSec, @AvgWatchTimeSec,
                     @TotalClipsViewed, @LikeToViewRatio, SYSUTCDATETIME())
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

        private async Task UpdateProfileAsync(UserProfileModel profile)
        {
            const string sql = @"
                UPDATE UserProfile
                SET TotalLikes        = @TotalLikes,
                    TotalWatchTimeSec = @TotalWatchTimeSec,
                    AvgWatchTimeSec   = @AvgWatchTimeSec,
                    TotalClipsViewed  = @TotalClipsViewed,
                    LikeToViewRatio   = @LikeToViewRatio,
                    LastUpdated       = SYSUTCDATETIME()
                WHERE UserId = @UserId
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

        private UserProfileModel MapUserProfile(SqlDataReader reader)
        {
            return new UserProfileModel
            {
                UserProfileId = reader.GetInt32(UserProfileModel_UserProfileId_Index),
                UserId = reader.GetInt32(UserProfileModel_UserId_Index),
                TotalLikes = reader.GetInt32(UserProfileModel_TotalLikes_Index),
                TotalWatchTimeSec = reader.GetInt64(UserProfileModel_TotalWatchTimeSec_Index),
                AvgWatchTimeSec = reader.GetDouble(UserProfileModel_AvgWatchTimeSec_Index),
                TotalClipsViewed = reader.GetInt32(UserProfileModel_TotalClipsViewed_Index),
                LikeToViewRatio = reader.GetDouble(UserProfileModel_LikeToViewRatio_Index),
                LastUpdated = reader.GetDateTime(UserProfileModel_LastUpdated_Index)
            };
        }
    }
}

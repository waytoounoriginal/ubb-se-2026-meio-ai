using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Raw SQL data access for the UserProfile table.
    /// </summary>
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

        private const int UserReelInteractionAggregate_TotalLikes_Index = 0;
        private const int UserReelInteractionAggregate_TotalWatchTimeSec_Index = 1;
        private const int UserReelInteractionAggregate_AvgWatchTimeSec_Index = 2;
        private const int UserReelInteractionAggregate_TotalClipsViewed_Index = 3;

        private readonly ISqlConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">Factory used to create SQL connections.</param>
        public ProfileRepository(ISqlConnectionFactory connectionFactory)
        {
            this._connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Calculates the like-to-view ratio based on total likes and total clips viewed.
        /// </summary>
        /// <param name="totalLikes">Total number of likes.</param>
        /// <param name="totalClipsViewed">Total number of clips viewed.</param>
        /// <returns>The ratio of likes to views, or 0 if no clips have been viewed.</returns>
        private double CalculateLikeToViewRatio(int totalLikes, int totalClipsViewed)
        {
            if (totalClipsViewed == 0)
                return 0;

            return (double)totalLikes / totalClipsViewed;
        }

        /// <inheritdoc />
        public async Task<UserProfileModel?> GetProfileAsync(int userId)
        {
            const string getProfileSql = @"
                SELECT UserProfileId, UserId, TotalLikes, TotalWatchTimeSec,
                       AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio, LastUpdated
                FROM UserProfile
                WHERE UserId = @UserId
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var getProfileCommand = new SqlCommand(getProfileSql, connection);
            getProfileCommand.Parameters.AddWithValue("@UserId", userId);

            await using var profileReader = await getProfileCommand.ExecuteReaderAsync();
            if (await profileReader.ReadAsync())
            {
                return this.MapUserProfile(profileReader);
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

            int totalLikes = reader.GetInt32(UserReelInteractionAggregate_TotalLikes_Index);
            long totalWatchTimeSec = reader.GetInt64(UserReelInteractionAggregate_TotalWatchTimeSec_Index);
            double avgWatchTimeSec = reader.GetDouble(UserReelInteractionAggregate_AvgWatchTimeSec_Index);
            int totalClipsViewed = reader.GetInt32(UserReelInteractionAggregate_TotalClipsViewed_Index);
            double likeToViewRatio = this.CalculateLikeToViewRatio(totalLikes, totalClipsViewed);

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

        /// <inheritdoc />
        public async Task UpsertProfileAsync(UserProfileModel profile)
        {
            var profileExists = await this.ProfileExistsAsync(profile.UserId);

            if (!profileExists)
            {
                await this.InsertProfileAsync(profile);
                return;
            }

            await this.UpdateProfileAsync(profile);
        }

        /// <summary>
        /// Checks whether a profile row exists for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the profile exists; otherwise false.</returns>
        private async Task<bool> ProfileExistsAsync(int userId)
        {
            const string checkProfileExistsSql = "SELECT 1 FROM UserProfile WHERE UserId = @UserId";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var checkProfileExistsCommand = new SqlCommand(checkProfileExistsSql, connection);
            checkProfileExistsCommand.Parameters.AddWithValue("@UserId", userId);
            var profileExistsResult = await checkProfileExistsCommand.ExecuteScalarAsync();
            return profileExistsResult != null;
        }

        /// <summary>
        /// Inserts a new engagement profile row.
        /// </summary>
        /// <param name="profile">The profile model to insert.</param>
        private async Task InsertProfileAsync(UserProfileModel profile)
        {
            const string insertProfileSql = @"
                INSERT INTO UserProfile
                    (UserId, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec,
                     TotalClipsViewed, LikeToViewRatio, LastUpdated)
                VALUES
                    (@UserId, @TotalLikes, @TotalWatchTimeSec, @AvgWatchTimeSec,
                     @TotalClipsViewed, @LikeToViewRatio, SYSUTCDATETIME())
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var insertProfileCommand = new SqlCommand(insertProfileSql, connection);
            insertProfileCommand.Parameters.AddWithValue("@UserId", profile.UserId);
            insertProfileCommand.Parameters.AddWithValue("@TotalLikes", profile.TotalLikes);
            insertProfileCommand.Parameters.AddWithValue("@TotalWatchTimeSec", profile.TotalWatchTimeSec);
            insertProfileCommand.Parameters.AddWithValue("@AvgWatchTimeSec", profile.AvgWatchTimeSec);
            insertProfileCommand.Parameters.AddWithValue("@TotalClipsViewed", profile.TotalClipsViewed);
            insertProfileCommand.Parameters.AddWithValue("@LikeToViewRatio", profile.LikeToViewRatio);
            await insertProfileCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Updates an existing engagement profile row.
        /// </summary>
        /// <param name="profile">The profile model with updated values.</param>
        private async Task UpdateProfileAsync(UserProfileModel profile)
        {
            const string updateProfileSql = @"
                UPDATE UserProfile
                SET TotalLikes        = @TotalLikes,
                    TotalWatchTimeSec = @TotalWatchTimeSec,
                    AvgWatchTimeSec   = @AvgWatchTimeSec,
                    TotalClipsViewed  = @TotalClipsViewed,
                    LikeToViewRatio   = @LikeToViewRatio,
                    LastUpdated       = SYSUTCDATETIME()
                WHERE UserId = @UserId
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var updateProfileCommand = new SqlCommand(updateProfileSql, connection);
            updateProfileCommand.Parameters.AddWithValue("@UserId", profile.UserId);
            updateProfileCommand.Parameters.AddWithValue("@TotalLikes", profile.TotalLikes);
            updateProfileCommand.Parameters.AddWithValue("@TotalWatchTimeSec", profile.TotalWatchTimeSec);
            updateProfileCommand.Parameters.AddWithValue("@AvgWatchTimeSec", profile.AvgWatchTimeSec);
            updateProfileCommand.Parameters.AddWithValue("@TotalClipsViewed", profile.TotalClipsViewed);
            updateProfileCommand.Parameters.AddWithValue("@LikeToViewRatio", profile.LikeToViewRatio);
            await updateProfileCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Maps a profile data-reader row to a <see cref="UserProfileModel"/>.
        /// </summary>
        /// <param name="reader">The data reader positioned on a profile row.</param>
        /// <returns>The mapped profile model.</returns>
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
                LastUpdated = reader.GetDateTime(UserProfileModel_LastUpdated_Index),
            };
        }
    }
}

using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Services
{
    /// <summary>
    /// Raw SQL data access for the Personality Match feature.
    /// Reads from shared UserMoviePreference, UserProfile, and User tables.
    /// Owner: Madi
    /// </summary>
    public class PersonalityMatchRepository : IPersonalityMatchRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public PersonalityMatchRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, List<UserMoviePreferenceModel>>> GetAllPreferencesExceptUserAsync(int excludeUserId)
        {
            const string sql = @"
                SELECT UserMoviePreferenceId, UserId, MovieId, Score, LastModified
                FROM   UserMoviePreference
                WHERE  UserId <> @ExcludeUserId;";

            var result = new Dictionary<int, List<UserMoviePreferenceModel>>();

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ExcludeUserId", excludeUserId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var pref = new UserMoviePreferenceModel
                {
                    UserMoviePreferenceId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    MovieId = reader.GetInt32(2),
                    Score = reader.GetDouble(3),
                    LastModified = reader.GetDateTime(4),
                };

                if (!result.ContainsKey(pref.UserId))
                {
                    result[pref.UserId] = new List<UserMoviePreferenceModel>();
                }

                result[pref.UserId].Add(pref);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<List<UserMoviePreferenceModel>> GetCurrentUserPreferencesAsync(int userId)
        {
            const string sql = @"
                SELECT UserMoviePreferenceId, UserId, MovieId, Score, LastModified
                FROM   UserMoviePreference
                WHERE  UserId = @UserId;";

            var results = new List<UserMoviePreferenceModel>();

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new UserMoviePreferenceModel
                {
                    UserMoviePreferenceId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    MovieId = reader.GetInt32(2),
                    Score = reader.GetDouble(3),
                    LastModified = reader.GetDateTime(4),
                });
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<UserProfileModel?> GetUserProfileAsync(int userId)
        {
            const string sql = @"
                SELECT UserProfileId, UserId, TotalLikes, TotalWatchTimeSec,
                       AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio, LastUpdated
                FROM   UserProfile
                WHERE  UserId = @UserId;";

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
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
                    LastUpdated = reader.GetDateTime(7),
                };
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<List<int>> GetRandomUserIdsAsync(int excludeUserId, int count)
        {
            const string sql = @"
                SELECT DISTINCT TOP (@Count) UserId
                FROM   UserMoviePreference
                WHERE  UserId <> @ExcludeUserId
                ORDER BY NEWID();";

            var ids = new List<int>();

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ExcludeUserId", excludeUserId);
            command.Parameters.AddWithValue("@Count", count);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt32(0));
            }

            return ids;
        }

        /// <inheritdoc />
        public async Task<string> GetUsernameAsync(int userId)
        {
            // The User table may not exist yet in all environments.
            // Fall back to "User {id}" if the table or row is missing.
            const string sql = @"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'User')
                BEGIN
                    SELECT Username FROM [User] WHERE UserId = @UserId;
                END";

            try
            {
                await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
                await using SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                object? result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return (string)result;
                }
            }
            catch
            {
                // User table may not exist — swallow and fall back
            }

            return $"User {userId}";
        }
    }
}

using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Raw SQL data access for the UserMoviePreference table.
    /// </summary>
    public class PreferenceRepository : IPreferenceRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreferenceRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">Factory used to create SQL connections.</param>
        public PreferenceRepository(ISqlConnectionFactory connectionFactory)
        {
            this._connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        public async Task<bool> PreferenceExistsAsync(int userId, int movieId)
        {
            const string checkPreferenceExistsSql = "SELECT 1 FROM UserMoviePreference WHERE UserId = @UserId AND MovieId = @MovieId";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var checkPreferenceExistsCommand = new SqlCommand(checkPreferenceExistsSql, connection);
            checkPreferenceExistsCommand.Parameters.AddWithValue("@UserId", userId);
            checkPreferenceExistsCommand.Parameters.AddWithValue("@MovieId", movieId);
            var preferenceExistsResult = await checkPreferenceExistsCommand.ExecuteScalarAsync();
            return preferenceExistsResult != null;
        }

        /// <inheritdoc />
        public async Task InsertPreferenceAsync(int userId, int movieId, double score)
        {
            const string insertPreferenceSql = @"
                INSERT INTO UserMoviePreference (UserId, MovieId, Score, LastModified, ChangeFromPreviousValue)
                VALUES (@UserId, @MovieId, @Score, SYSUTCDATETIME(), 0)
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var insertPreferenceCommand = new SqlCommand(insertPreferenceSql, connection);
            insertPreferenceCommand.Parameters.AddWithValue("@UserId", userId);
            insertPreferenceCommand.Parameters.AddWithValue("@MovieId", movieId);
            insertPreferenceCommand.Parameters.AddWithValue("@Score", score);
            await insertPreferenceCommand.ExecuteNonQueryAsync();
        }

        /// <inheritdoc />
        public async Task UpdatePreferenceAsync(int userId, int movieId, double boost)
        {
            const string updatePreferenceSql = @"
                UPDATE UserMoviePreference
                SET Score = Score + @Boost,
                    LastModified = SYSUTCDATETIME(),
                    ChangeFromPreviousValue = @Boost
                WHERE UserId = @UserId AND MovieId = @MovieId
            ";

            await using var connection = await this._connectionFactory.CreateConnectionAsync();
            await using var updatePreferenceCommand = new SqlCommand(updatePreferenceSql, connection);
            updatePreferenceCommand.Parameters.AddWithValue("@UserId", userId);
            updatePreferenceCommand.Parameters.AddWithValue("@MovieId", movieId);
            updatePreferenceCommand.Parameters.AddWithValue("@Boost", boost);
            await updatePreferenceCommand.ExecuteNonQueryAsync();
        }
    }
}

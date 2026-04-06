using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Raw SQL data access for the UserMoviePreference table.
    /// </summary>
    public class PreferenceRepository : IPreferenceRepository
    {
        private const double LikeBoostAmount = 1.5;
        private readonly ISqlConnectionFactory _connectionFactory;

        public PreferenceRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task BoostPreferenceOnLikeAsync(int userId, int movieId)
        {
            var exists = await PreferenceExistsAsync(userId, movieId);

            if (!exists)
            {
                await InsertPreferenceAsync(userId, movieId, LikeBoostAmount);
                return;
            }

            await UpdatePreferenceAsync(userId, movieId, LikeBoostAmount);
        }

        private async Task<bool> PreferenceExistsAsync(int userId, int movieId)
        {
            const string sql = "SELECT 1 FROM UserMoviePreference WHERE UserId = @UserId AND MovieId = @MovieId";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@MovieId", movieId);
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }

        private async Task InsertPreferenceAsync(int userId, int movieId, double score)
        {
            const string sql = @"
                INSERT INTO UserMoviePreference (UserId, MovieId, Score, LastModified, ChangeFromPreviousValue)
                VALUES (@UserId, @MovieId, @Score, SYSUTCDATETIME(), 0)
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@MovieId", movieId);
            command.Parameters.AddWithValue("@Score", score);
            await command.ExecuteNonQueryAsync();
        }

        private async Task UpdatePreferenceAsync(int userId, int movieId, double boost)
        {
            const string sql = @"
                UPDATE UserMoviePreference
                SET Score = Score + @Boost,
                    LastModified = SYSUTCDATETIME(),
                    ChangeFromPreviousValue = @Boost
                WHERE UserId = @UserId AND MovieId = @MovieId
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@MovieId", movieId);
            command.Parameters.AddWithValue("@Boost", boost);
            await command.ExecuteNonQueryAsync();
        }
    }
}

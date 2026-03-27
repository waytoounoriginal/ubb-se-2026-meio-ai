using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories
{
    /// <summary>
    /// Raw SQL data access for Tudor's writes to the UserMoviePreference table.
    /// Implements the +1.5 score boost on reel like (per Tudor.md cross-team spec).
    /// Owner: Tudor
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
            const string sql = @"
                IF EXISTS (SELECT 1 FROM UserMoviePreference WHERE UserId = @UserId AND MovieId = @MovieId)
                BEGIN
                    UPDATE UserMoviePreference
                    SET Score        = Score + @Boost,
                        LastModified = SYSUTCDATETIME(),
                        ChangeFromPreviousValue = @Boost
                    WHERE UserId = @UserId AND MovieId = @MovieId;
                END
                ELSE
                BEGIN
                    INSERT INTO UserMoviePreference (UserId, MovieId, Score, LastModified, ChangeFromPreviousValue)
                    VALUES (@UserId, @MovieId, @Boost, SYSUTCDATETIME(), 0);
                END
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@MovieId", movieId);
            command.Parameters.AddWithValue("@Boost", LikeBoostAmount);
            await command.ExecuteNonQueryAsync();
        }
    }
}

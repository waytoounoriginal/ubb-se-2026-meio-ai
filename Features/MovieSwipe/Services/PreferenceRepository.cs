using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Services
{
    /// <summary>
    /// SQL Server implementation of <see cref="IPreferenceRepository"/>.
    /// Uses raw ADO.NET — no ORM.
    /// Owner: Bogdan
    /// </summary>
    public class PreferenceRepository : IPreferenceRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public PreferenceRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        public async Task<UserMoviePreferenceModel?> GetPreferenceAsync(int userId, int movieId)
        {
            const string sql = @"
                SELECT UserMoviePreferenceId, UserId, MovieId, Score, LastModified
                FROM   UserMoviePreference
                WHERE  UserId = @UserId AND MovieId = @MovieId;";

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@MovieId", movieId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserMoviePreferenceModel
                {
                    UserMoviePreferenceId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    MovieId = reader.GetInt32(2),
                    Score = reader.GetDouble(3),
                    LastModified = reader.GetDateTime(4),
                };
            }

            return null;
        }

        /// <inheritdoc />
        public async Task UpsertPreferenceAsync(UserMoviePreferenceModel preference)
        {
            // MERGE ensures atomic insert-or-update.
            // If no row exists → insert at (0 + Score). If row exists → add Score to current.
            const string sql = @"
                MERGE UserMoviePreference AS target
                USING (SELECT @UserId AS UserId, @MovieId AS MovieId) AS source
                ON    target.UserId = source.UserId AND target.MovieId = source.MovieId
                WHEN MATCHED THEN
                    UPDATE SET Score        = target.Score + @ScoreDelta,
                               LastModified = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (UserId, MovieId, Score, LastModified)
                    VALUES (@UserId, @MovieId, @ScoreDelta, SYSUTCDATETIME());";

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", preference.UserId);
            command.Parameters.AddWithValue("@MovieId", preference.MovieId);
            command.Parameters.AddWithValue("@ScoreDelta", preference.Score);

            await command.ExecuteNonQueryAsync();
        }

        /// <inheritdoc />
        public async Task<List<MovieCardModel>> GetUnswipedMoviesAsync(int userId, int count)
        {
            // Reads from the external Movie table, filtering out movies
            // that already have a UserMoviePreference row for this user.
            const string sql = @"
                SELECT TOP (@Count) m.MovieId, m.Title, m.PosterUrl, m.PrimaryGenre
                FROM   Movie m
                LEFT JOIN UserMoviePreference ump
                    ON ump.MovieId = m.MovieId AND ump.UserId = @UserId
                WHERE  ump.UserMoviePreferenceId IS NULL
                ORDER BY NEWID();";

            var results = new List<MovieCardModel>();

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Count", count);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new MovieCardModel
                {
                    MovieId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    PosterUrl = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    PrimaryGenre = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                });
            }

            return results;
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
        public async Task<List<int>> GetUnswipedMovieIdsAsync(int userId)
        {
            // Returns MovieIds from the Movie table that the user has NOT swiped on.
            const string sql = @"
                SELECT m.MovieId
                FROM   Movie m
                LEFT JOIN UserMoviePreference ump
                    ON ump.MovieId = m.MovieId AND ump.UserId = @UserId
                WHERE  ump.UserMoviePreferenceId IS NULL;";

            var ids = new List<int>();

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt32(0));
            }

            return ids;
        }
    }
}

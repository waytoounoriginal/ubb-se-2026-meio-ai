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
        /// <summary> The connection factory for creating SQL connections. </summary>
        private readonly ISqlConnectionFactory _connectionFactory;

        /// <summary> Initializes a new instance of the <see cref="PreferenceRepository"/> class. </summary>
        /// <param name="connectionFactory">The SQL connection factory.</param>
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
                const int idColumnIndex = 0;
                const int userColumnIndex = 1;
                const int movieColumnIndex = 2;
                const int scoreColumnIndex = 3;
                const int dateColumnIndex = 4;

                return new UserMoviePreferenceModel
                {
                    UserMoviePreferenceId = reader.GetInt32(idColumnIndex),
                    UserId = reader.GetInt32(userColumnIndex),
                    MovieId = reader.GetInt32(movieColumnIndex),
                    Score = reader.GetDouble(scoreColumnIndex),
                    LastModified = reader.GetDateTime(dateColumnIndex),
                };
            }

            return null;
        }

        /// <inheritdoc />
        public async Task UpsertPreferenceAsync(UserMoviePreferenceModel preference)
        {
            const string sql = @"
                MERGE UserMoviePreference AS target
                USING (SELECT @UserId AS UserId, @MovieId AS MovieId) AS source
                ON    target.UserId = source.UserId AND target.MovieId = source.MovieId
                WHEN MATCHED THEN
                    UPDATE SET Score        = target.Score + @ScoreDelta,
                               LastModified = SYSUTCDATETIME(),
                               ChangeFromPreviousValue = @ChangeFromPreviousValue
                WHEN NOT MATCHED THEN
                    INSERT (UserId, MovieId, Score, LastModified, ChangeFromPreviousValue)
                    VALUES (@UserId, @MovieId, @ScoreDelta, SYSUTCDATETIME(), @ChangeFromPreviousValue);";

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", preference.UserId);
            command.Parameters.AddWithValue("@MovieId", preference.MovieId);
            command.Parameters.AddWithValue("@ScoreDelta", preference.Score);
            command.Parameters.AddWithValue("@ChangeFromPreviousValue", preference.ChangeFromPreviousValue ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        /// <inheritdoc />
        public async Task<List<MovieCardModel>> GetMovieFeedAsync(int userId, int count)
        {
            const string sql = @"
                SELECT TOP (@Count) m.MovieId, m.Title, m.PosterUrl, m.PrimaryGenre
                FROM   Movie m
                LEFT JOIN UserMoviePreference ump
                    ON ump.MovieId = m.MovieId AND ump.UserId = @UserId
                ORDER BY 
                    CASE WHEN ump.UserMoviePreferenceId IS NULL THEN 0 ELSE 1 END ASC,
                    ISNULL(ump.LastModified, '2000-01-01') ASC,
                    NEWID();";

            var results = new List<MovieCardModel>();

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Count", count);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                const int idIndex = 0;
                const int titleIndex = 1;
                const int posterIndex = 2;
                const int genreIndex = 3;

                results.Add(new MovieCardModel
                {
                    MovieId = reader.GetInt32(idIndex),
                    Title = reader.GetString(titleIndex),
                    PosterUrl = reader.IsDBNull(posterIndex) ? string.Empty : reader.GetString(posterIndex),
                    PrimaryGenre = reader.IsDBNull(genreIndex) ? string.Empty : reader.GetString(genreIndex),
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
                const int idIdx = 0;
                const int userIdx = 1;
                const int movieIdx = 2;
                const int scoreIdx = 3;
                const int dateIdx = 4;

                var preference = new UserMoviePreferenceModel
                {
                    UserMoviePreferenceId = reader.GetInt32(idIdx),
                    UserId = reader.GetInt32(userIdx),
                    MovieId = reader.GetInt32(movieIdx),
                    Score = reader.GetDouble(scoreIdx),
                    LastModified = reader.GetDateTime(dateIdx),
                };

                if (!result.ContainsKey(preference.UserId))
                {
                    result[preference.UserId] = new List<UserMoviePreferenceModel>();
                }

                result[preference.UserId].Add(preference);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<List<int>> GetUnswipedMovieIdsAsync(int userId)
        {
            const string sql = @"
                SELECT m.MovieId
                FROM   Movie m
                LEFT JOIN UserMoviePreference ump
                    ON ump.MovieId = m.MovieId AND ump.UserId = @UserId
                WHERE  ump.UserMoviePreferenceId IS NULL;";

            var movieIds = new List<int>();

            await using SqlConnection connection = await _connectionFactory.CreateConnectionAsync();
            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                const int firstColumn = 0;
                movieIds.Add(reader.GetInt32(firstColumn));
            }

            return movieIds;
        }
    }
}
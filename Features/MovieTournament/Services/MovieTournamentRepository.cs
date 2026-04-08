using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Services
{
    /// <summary>
    /// Repository responsible for querying and updating movie tournament data
    /// in the SQL database, including pool retrieval and score boosting.
    /// </summary>
    public class MovieTournamentRepository : IMovieTournamentRepository
    {
        private const string UserIdParameterName = "@UserId";
        private const string MovieIdParameterName = "@MovieId";
        private const string PoolSizeParameterName = "@PoolSize";
        private const string ScoreBoostParameterName = "@ScoreBoost";

        private readonly ISqlConnectionFactory connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MovieTournamentRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">
        /// The factory used to create SQL database connections.
        /// </param>
        public MovieTournamentRepository(ISqlConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        /// <inheritdoc/>
        public async Task<int> GetTournamentPoolSizeAsync(int userId)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM UserMoviePreference
                WHERE UserId = @UserId AND ChangeFromPreviousValue > 0;
            ";

            await using var connection = await this.connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue(UserIdParameterName, userId);

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }

        /// <inheritdoc/>
        public async Task<List<MovieCardModel>> GetTournamentPoolAsync(int userId, int poolSize)
        {
            const string sql = @"
                SELECT TOP (@PoolSize) m.MovieId, m.Title, m.PosterUrl, m.ReleaseYear, m.PrimaryGenre
                FROM Movie m
                INNER JOIN UserMoviePreference ump ON m.MovieId = ump.MovieId
                WHERE ump.UserId = @UserId AND ump.ChangeFromPreviousValue > 0
                ORDER BY ump.LastModified DESC;
            ";

            await using var connection = await this.connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue(UserIdParameterName, userId);
            command.Parameters.AddWithValue(PoolSizeParameterName, poolSize);

            var movies = new List<MovieCardModel>();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                movies.Add(new MovieCardModel
                {
                    MovieId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    PosterUrl = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    ReleaseYear = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                    Genre = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                });
            }

            return movies;
        }

        /// <inheritdoc/>
        public async Task BoostMovieScoreAsync(int userId, int movieId, double scoreBoost)
        {
            const string sql = @"
                UPDATE UserMoviePreference
                SET Score = Score + @ScoreBoost,
                    LastModified = SYSUTCDATETIME(),
                    ChangeFromPreviousValue = @ScoreBoost
                WHERE UserId = @UserId AND MovieId = @MovieId;
            ";

            await using var connection = await this.connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue(UserIdParameterName, userId);
            command.Parameters.AddWithValue(MovieIdParameterName, movieId);
            command.Parameters.AddWithValue(ScoreBoostParameterName, scoreBoost);

            await command.ExecuteNonQueryAsync();
        }
    }
}
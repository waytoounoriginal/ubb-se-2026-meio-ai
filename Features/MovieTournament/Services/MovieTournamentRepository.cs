using System.Data;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Services
{
    public class MovieTournamentRepository : IMovieTournamentRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public MovieTournamentRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> GetTournamentPoolSizeAsync(int userId)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM UserMoviePreference
                WHERE UserId = @UserId AND ChangeFromPreviousValue > 0;
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<List<Models.MovieCard>> GetTournamentPoolAsync(int userId, int poolSize)
        {
            const string sql = @"
                SELECT TOP (@PoolSize) m.MovieId, m.Title, m.PosterUrl, m.ReleaseYear
                FROM Movie m
                INNER JOIN UserMoviePreference ump ON m.MovieId = ump.MovieId
                WHERE ump.UserId = @UserId AND ump.ChangeFromPreviousValue > 0
                ORDER BY ump.LastModified DESC;
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@PoolSize", poolSize);

            var movies = new List<Models.MovieCard>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                movies.Add(new Models.MovieCard(
                    reader.GetInt32(0),   // MovieId
                    reader.GetString(1),  // Title
                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2), // PosterUrl
                    reader.IsDBNull(3) ? 0 : reader.GetInt32(3)              // ReleaseYear
                ));
            }

            return movies;
        }

        public async Task BoostMovieScoreAsync(int userId, int movieId, float scoreBoost)
        {
            // Boosts the score in the UserMoviePreference table
            const string sql = @"
                UPDATE UserMoviePreference
                SET Score = Score + @ScoreBoost,
                    LastModified = SYSUTCDATETIME()
                WHERE UserId = @UserId AND MovieId = @MovieId;
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@MovieId", movieId);
            command.Parameters.AddWithValue("@ScoreBoost", scoreBoost);

            await command.ExecuteNonQueryAsync();
        }
    }
}

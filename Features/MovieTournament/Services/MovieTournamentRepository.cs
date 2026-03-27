using System.Data;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;


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

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }

        public async Task<List<MovieCardModel>> GetTournamentPoolAsync(int userId, int poolSize)
        {
            const string sql = @"
                SELECT TOP (@PoolSize) m.MovieId, m.Title, m.PosterUrl, m.ReleaseYear, m.PrimaryGenre
                FROM Movie m
                INNER JOIN UserMoviePreference ump ON m.MovieId = ump.MovieId
                WHERE ump.UserId = @UserId AND ump.ChangeFromPreviousValue > 0
                ORDER BY ump.LastModified DESC;
            ";

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@PoolSize", poolSize);

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

        public async Task BoostMovieScoreAsync(int userId, int movieId, double scoreBoost)
        {
            
            const string sql = @"USE MeioAiDb;
                UPDATE UserMoviePreference
                SET Score = Score + @ScoreBoost,
                    LastModified = SYSUTCDATETIME(),
                    ChangeFromPreviousValue = @ScoreBoost
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

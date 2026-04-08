using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Core.Repositories
{
    /// <summary>
    /// The concrete implementation of the IMovieRepository
    /// </summary>
    public class MovieRepository : IMovieRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public MovieRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<MovieCardModel>> SearchTop10MoviesAsync(string partialMovieName)
        {
            var newMovieResults = new List<MovieCardModel>();

            await using var connection = await _connectionFactory.CreateConnectionAsync();

            string sqlInstruction = "SELECT TOP 10 MovieId, Title, PosterUrl, PrimaryGenre, ReleaseYear, Description FROM Movie WHERE Title LIKE @SearchTerm";
            await using var sqlCommand = new SqlCommand(sqlInstruction, connection);

            // Add the search parameter securely
            string searchParameter = "@SearchTerm";
            string searchedText = $"%{partialMovieName}%";
            sqlCommand.Parameters.AddWithValue(searchParameter, searchedText);

            await using var sqlCommandOutputReader = await sqlCommand.ExecuteReaderAsync();

            // Define column names
            string movieIdField = "MovieId", titleField = "Title", posterField = "PosterUrl";
            string primaryGenreField = "PrimaryGenre", releaseYearField = "ReleaseYear", descriptionField = "Description";
            const int nullId = 0;

            // Read the data and map it to our C# objects
            while (await sqlCommandOutputReader.ReadAsync())
            {
                newMovieResults.Add(new MovieCardModel
                {
                    MovieId = sqlCommandOutputReader.GetInt32(sqlCommandOutputReader.GetOrdinal(movieIdField)),
                    Title = sqlCommandOutputReader.GetString(sqlCommandOutputReader.GetOrdinal(titleField)),
                    PosterUrl = sqlCommandOutputReader.IsDBNull(sqlCommandOutputReader.GetOrdinal(posterField)) ? string.Empty : sqlCommandOutputReader.GetString(sqlCommandOutputReader.GetOrdinal(posterField)),
                    PrimaryGenre = sqlCommandOutputReader.IsDBNull(sqlCommandOutputReader.GetOrdinal(primaryGenreField)) ? string.Empty : sqlCommandOutputReader.GetString(sqlCommandOutputReader.GetOrdinal(primaryGenreField)),
                    ReleaseYear = sqlCommandOutputReader.IsDBNull(sqlCommandOutputReader.GetOrdinal(releaseYearField)) ? nullId : sqlCommandOutputReader.GetInt32(sqlCommandOutputReader.GetOrdinal(releaseYearField)),
                    Synopsis = sqlCommandOutputReader.IsDBNull(sqlCommandOutputReader.GetOrdinal(descriptionField)) ? string.Empty : sqlCommandOutputReader.GetString(sqlCommandOutputReader.GetOrdinal(descriptionField))
                });
            }

            return newMovieResults;
        }
    }
}
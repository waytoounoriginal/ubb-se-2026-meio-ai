using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Core.Repositories;

namespace ubb_se_2026_meio_ai.Core.Services
{
    /// <summary>
    /// The concrete implementation of IMovieService.
    /// Owner: Alex
    /// </summary>
    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;

        public MovieService(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }

        public async Task<List<MovieCardModel>> SearchTop10MoviesAsync(string partialMovieName)
        {
            // Don't query the database if the string is empty
            if (string.IsNullOrWhiteSpace(partialMovieName))
            {
                return new List<MovieCardModel>();
            }

            return await _movieRepository.SearchTop10MoviesAsync(partialMovieName);
        }
    }
}

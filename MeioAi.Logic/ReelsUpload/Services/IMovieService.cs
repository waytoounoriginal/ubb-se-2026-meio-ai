using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Core.Services
{
    /// <summary>
    /// The class used for getting information regarding the movies.
    /// </summary>
    public interface IMovieService
    {
        /// <summary>
        /// Searches for movies based on a partial title.
        /// Owner: Alex
        /// </summary>
        Task<List<MovieCardModel>> SearchTop10MoviesAsync(string partialMovieName);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Core.Repositories
{
    public interface IMovieRepository
    {
        /// <summary>
        /// Queries the database for the top 10 movies matching a partial title.
        /// </summary>
        Task<List<MovieCardModel>> SearchTop10MoviesAsync(string partialMovieName);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Models
{
    public class MovieCard
    {
        public int MovieId { get; set; }
        public string Title { get; set; }
        public string PosterUrl { get; set; }
        public int ReleaseYear { get; set; }

        public MovieCard(int movieId, string title, string posterUrl, int releaseYear)
        {
            MovieId = movieId;
            Title = title;
            PosterUrl = posterUrl;
            ReleaseYear = releaseYear;
        }
    }
}

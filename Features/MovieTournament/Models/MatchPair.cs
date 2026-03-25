using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Models
{
    public class MatchPair
    {
        public MovieCard MovieA { get; set; }
        public MovieCard MovieB { get; set; }
        public int? WinnerId { get; set; }

        public MatchPair(MovieCard movieA, MovieCard movieB)
        {
            MovieA = movieA;
            MovieB = movieB;
            WinnerId = null;
        }

        public bool IsCompleted()
        {
            return WinnerId != null;
        }

        public bool IsBye()
        {
            return MovieB == null;
        }
    }
}

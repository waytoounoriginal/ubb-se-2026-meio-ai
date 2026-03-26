using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Models
{
    public class MatchPair
    {
        public MovieCardModel MovieA { get; set; }
        public MovieCardModel MovieB { get; set; }
        public int? WinnerId { get; set; }

        public MatchPair(MovieCardModel movieA, MovieCardModel movieB)
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

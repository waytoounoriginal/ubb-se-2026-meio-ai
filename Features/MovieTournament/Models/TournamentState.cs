using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Models
{
    public class TournamentState
    {
        public List<MatchPair> PendingMatches { get; set; }
        public List<MatchPair> CompletedMatches { get; set; }
        public int CurrentRound { get; set; }
        public List<MovieCardModel> CurrentRoundWinners { get; set; }

        public TournamentState()
        {
            PendingMatches = new List<MatchPair>();
            CompletedMatches = new List<MatchPair>();
            CurrentRoundWinners = new List<MovieCardModel>();
            CurrentRound = 1;
        }
    }
}

using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.MovieTournament.Models;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Services
{

    public interface ITournamentLogicService
    {
        TournamentState CurrentState { get; }
        bool IsTournamentActive { get; }

        Task StartTournamentAsync(int userId, int poolSize);
        Task AdvanceWinnerAsync(int userId, int winnerId);
        void ResetTournament();

        MatchPair? GetCurrentMatch();
        bool IsTournamentComplete();
        MovieCardModel GetFinalWinner();
    }
}

using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Services
{
    /// <summary>
    /// Core bracket generation and tournament progression logic.
    /// Owner: Gabi
    /// </summary>
    public interface ITournamentLogicService
    {
        Models.TournamentState CurrentState { get; }
        bool IsTournamentActive { get; }

        Task StartTournamentAsync(int userId, int poolSize);
        Task AdvanceWinnerAsync(int userId, int winnerId);
        void ResetTournament();

        Models.MatchPair GetCurrentMatch();
        bool IsTournamentComplete();
        Models.MovieCard GetFinalWinner();
    }
}

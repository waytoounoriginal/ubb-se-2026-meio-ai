using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Services
{
    /// <summary>
    /// Core bracket generation and tournament progression logic.
    /// Owner: Gabi
    /// </summary>
    public interface ITournamentLogicService
    {
        Task<IList<(MovieCardModel MovieA, MovieCardModel MovieB)>> GenerateBracketAsync(int userId, int bracketSize);
        Task AdvanceWinnerAsync(int tournamentId, int winnerMovieId);
    }
}

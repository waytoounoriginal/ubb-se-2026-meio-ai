namespace ubb_se_2026_meio_ai.Features.MovieTournament.Services
{
    /// <summary>
    /// Data access for tournament state and results.
    /// Owner: Gabi
    /// </summary>
    public interface IMovieTournamentRepository
    {
        Task SaveTournamentResultAsync(int userId, int winnerMovieId, int loserMovieId, int round);
        Task<IList<int>> GetPastWinnersAsync(int userId, int count);
    }
}

using ubb_se_2026_meio_ai.Core.Models;
namespace ubb_se_2026_meio_ai.Features.MovieTournament.Services
{
    /// <summary>
    /// Data access for tournament state and results.
    /// Owner: Gabi
    /// </summary>
    public interface IMovieTournamentRepository
    {
        Task<int> GetTournamentPoolSizeAsync(int userId);
        Task<List<MovieCardModel>> GetTournamentPoolAsync(int userId, int poolSize);
        Task BoostMovieScoreAsync(int userId, int movieId, double scoreBoost);
    }
}

using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Orchestrates user–reel interactions by delegating persistence to
    /// <see cref="IInteractionRepository"/> and preference boosts to
    /// <see cref="IPreferenceRepository"/>.
    /// Owner: Tudor
    /// </summary>
    public class ReelInteractionService : IReelInteractionService
    {
        private readonly IInteractionRepository _interactionRepository;
        private readonly IPreferenceRepository _preferenceRepository;

        public ReelInteractionService(
            IInteractionRepository interactionRepository,
            IPreferenceRepository preferenceRepository)
        {
            _interactionRepository = interactionRepository;
            _preferenceRepository = preferenceRepository;
        }

        /// <summary>
        /// Toggles the IsLiked flag and, when a reel transitions to liked,
        /// boosts the associated movie's preference score by +1.5.
        /// </summary>
        public async Task ToggleLikeAsync(int userId, int reelId)
        {
            var existing = await _interactionRepository.GetInteractionAsync(userId, reelId);
            bool wasLiked = existing?.IsLiked ?? false;

            await _interactionRepository.ToggleLikeAsync(userId, reelId);

            // Boost preference only when transitioning from unliked → liked
            if (!wasLiked)
            {
                int? movieId = await _interactionRepository.GetReelMovieIdAsync(reelId);
                if (movieId.HasValue)
                {
                    await _preferenceRepository.BoostPreferenceOnLikeAsync(userId, movieId.Value);
                }
            }
        }

        public async Task RecordViewAsync(int userId, int reelId, double watchDurationSec, double watchPercentage)
        {
            await _interactionRepository.UpdateViewDataAsync(userId, reelId, watchDurationSec, watchPercentage);
        }

        public async Task<UserReelInteractionModel?> GetInteractionAsync(int userId, int reelId)
        {
            return await _interactionRepository.GetInteractionAsync(userId, reelId);
        }

        public async Task<int> GetLikeCountAsync(int reelId)
        {
            return await _interactionRepository.GetLikeCountAsync(reelId);
        }
    }
}

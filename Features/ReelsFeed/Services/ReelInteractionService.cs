using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Orchestrates user–reel interactions by delegating persistence to
    /// <see cref="IInteractionRepository"/> and preference boosts to
    /// <see cref="IPreferenceRepository"/>.
    /// Owner: Tudor.
    /// </summary>
    public class ReelInteractionService : IReelInteractionService
    {
        private const double LikeBoostAmount = 1.5;
        private readonly IInteractionRepository _interactionRepository;
        private readonly IPreferenceRepository _preferenceRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReelInteractionService"/> class.
        /// </summary>
        /// <param name="interactionRepository">Repository used for reel interaction persistence.</param>
        /// <param name="preferenceRepository">Repository used for updating movie preference boosts.</param>
        public ReelInteractionService(
            IInteractionRepository interactionRepository,
            IPreferenceRepository preferenceRepository)
        {
            this._interactionRepository = interactionRepository;
            this._preferenceRepository = preferenceRepository;
        }

        /// <inheritdoc />
        public async Task ToggleLikeAsync(int userId, int reelId)
        {
            // Implementation detail: apply a preference boost only on unliked -> liked transitions.
            var existingInteraction = await this._interactionRepository.GetInteractionAsync(userId, reelId);
            bool wasLiked = existingInteraction?.IsLiked ?? false;

            await this._interactionRepository.ToggleLikeAsync(userId, reelId);

            // Boost preference only when transitioning from unliked → liked
            if (!wasLiked)
            {
                int? associatedMovieId = await this._interactionRepository.GetReelMovieIdAsync(reelId);
                if (associatedMovieId.HasValue)
                {
                    await this.BoostPreferenceOnLikeAsync(userId, associatedMovieId.Value);
                }
            }
        }

        /// <summary>
        /// Boosts user's preference for a movie by applying upsert logic:
        /// if preference doesn't exist, insert with boost amount;  otherwise, add boost amount.
        /// </summary>
        private async Task BoostPreferenceOnLikeAsync(int userId, int movieId)
        {
            var preferenceExists = await this._preferenceRepository.PreferenceExistsAsync(userId, movieId);

            if (!preferenceExists)
            {
                await this._preferenceRepository.InsertPreferenceAsync(userId, movieId, LikeBoostAmount);
            }
            else
            {
                await this._preferenceRepository.UpdatePreferenceAsync(userId, movieId, LikeBoostAmount);
            }
        }

        /// <inheritdoc />
        public async Task RecordViewAsync(int userId, int reelId, double watchDurationSec, double watchPercentage)
        {
            await this._interactionRepository.UpdateViewDataAsync(userId, reelId, watchDurationSec, watchPercentage);
        }

        /// <inheritdoc />
        public async Task<UserReelInteractionModel?> GetInteractionAsync(int userId, int reelId)
        {
            return await this._interactionRepository.GetInteractionAsync(userId, reelId);
        }

        /// <inheritdoc />
        public async Task<int> GetLikeCountAsync(int reelId)
        {
            return await this._interactionRepository.GetLikeCountAsync(reelId);
        }
    }
}

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Models
{
    /// <summary>
    /// Display model for a user's top movie preference entry, enriched with the movie title.
    /// Replaces the raw UserMoviePreferenceModel in the detail view so we show names, not IDs.
    /// Owner: Madi
    /// </summary>
    public class MoviePreferenceDisplayModel
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public double Score { get; set; }

        /// <summary>
        /// True for the entry with the highest score in this user's top-5 list.
        /// Drives the red highlight border in the detail view.
        /// </summary>
        public bool IsBestMovie { get; set; }
    }
}

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Models
{
    /// <summary>
    /// In-memory DTO holding a matched user's data and compatibility score.
    /// NOT persisted to the database.
    /// Owner: Madi
    /// </summary>
    public class MatchResult
    {
        public int MatchedUserId { get; set; }
        public string MatchedUsername { get; set; } = string.Empty;

        /// <summary>Compatibility percentage 0–100.</summary>
        public double MatchScore { get; set; }

        /// <summary>
        /// Hardcoded Facebook nickname — NOT stored in the database.
        /// Will be moved to a DB column in a future sprint.
        /// </summary>
        public string FacebookAccount { get; set; } = string.Empty;
    }
}

namespace ubb_se_2026_meio_ai.Core.Models
{
    /// <summary>
    /// Represents a single scraping job executed by the admin.
    /// </summary>
    public class ScrapeJobModel
    {
        public int ScrapeJobId { get; set; }
        public string SearchQuery { get; set; } = string.Empty;
        public int MaxResults { get; set; }
        public string Status { get; set; } = "pending";
        public int MoviesFound { get; set; }
        public int ReelsCreated { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

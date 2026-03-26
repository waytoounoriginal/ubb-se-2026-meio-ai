namespace ubb_se_2026_meio_ai.Core.Models
{
    /// <summary>
    /// A single log entry belonging to a scrape job.
    /// </summary>
    public class ScrapeJobLogModel
    {
        public long LogId { get; set; }
        public int ScrapeJobId { get; set; }
        public string Level { get; set; } = "Info";
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

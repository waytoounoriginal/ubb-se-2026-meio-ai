namespace ubb_se_2026_meio_ai.Core.Models
{
    
    public class ScrapeJobLogModel
    {
        public long LogId { get; set; }
        public int ScrapeJobId { get; set; }
        public string Level { get; set; } = "Info";
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

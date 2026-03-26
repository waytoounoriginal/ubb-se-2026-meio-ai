namespace ubb_se_2026_meio_ai.Core.Models
{
    public class MusicTrackModel
    {
        public int MusicTrackId { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
    }
}

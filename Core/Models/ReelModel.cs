namespace ubb_se_2026_meio_ai.Core.Models
{
    public class ReelModel
    {
        public int ReelId { get; set; }
        public int MovieId { get; set; }
        public int CreatorUserId { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public double FeatureDurationSeconds { get; set; }
        public string? CropDataJson { get; set; }
        public int? BackgroundMusicId { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastEditedAt { get; set; }
    }
}

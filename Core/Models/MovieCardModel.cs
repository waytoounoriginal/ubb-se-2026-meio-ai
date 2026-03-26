namespace ubb_se_2026_meio_ai.Core.Models
{
    public class MovieCardModel
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public string Synopsis { get; set; } = string.Empty;
    }
}

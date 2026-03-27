namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Models
{
    /// <summary>
    /// Local DTO holding in-progress crop + music edits before saving.
    /// Owner: Beatrice
    /// </summary>
    public class VideoEditMetadata
    {
        public int CropX { get; set; }
        public int CropY { get; set; }
        public int CropWidth { get; set; } = 1920;
        public int CropHeight { get; set; } = 1080;
        public int? SelectedMusicTrackId { get; set; }

        // Music parameters
        public double MusicStartTime { get; set; }
        public double MusicDuration { get; set; } = 30.0;
        public double MusicVolume { get; set; } = 80.0;

        public string ToCropDataJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                x = CropX,
                y = CropY,
                width = CropWidth,
                height = CropHeight,
                musicStartTime = MusicStartTime,
                musicDuration = MusicDuration,
                musicVolume = MusicVolume
            });
        }
    }
}

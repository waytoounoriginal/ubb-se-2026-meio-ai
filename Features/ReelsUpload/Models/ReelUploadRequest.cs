namespace ubb_se_2026_meio_ai.Features.ReelsUpload.Models
{
    public class ReelUploadRequest
    {
        public string LocalFilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public int UploaderUserId { get; set; }
        public int? MovieId { get; set; }
    }
}

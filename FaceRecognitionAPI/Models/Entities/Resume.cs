namespace FaceRecognitionAPI.Models.Entities
{
    public class Resume
    {
        public int ResumeId { get; set; }

        // Foreign key → Users table
        public int UserId { get; set; }

        public string FileName { get; set; } = string.Empty;

        // Relative path inside wwwroot/uploads/resumes/
        public string FilePath { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User User { get; set; } = null!;
    }
}

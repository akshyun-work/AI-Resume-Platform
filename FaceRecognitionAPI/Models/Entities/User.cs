namespace FaceRecognitionAPI.Models.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public FaceEmbedding? FaceEmbedding { get; set; }
        public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
    }
}

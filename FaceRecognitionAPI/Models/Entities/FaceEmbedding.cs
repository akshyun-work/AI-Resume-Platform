namespace FaceRecognitionAPI.Models.Entities
{
    public class FaceEmbedding
    {
        // Primary key AND foreign key → Users table (one-to-one)
        public int UserId { get; set; }
        public string Embedding { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User User { get; set; } = null!;
    }
}

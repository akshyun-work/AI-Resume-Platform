namespace FaceRecognitionAPI.Models.Entities
{
    public class FaceEmbedding
    {
        public int UserId { get; set; }
        public string Embedding { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
    }
}

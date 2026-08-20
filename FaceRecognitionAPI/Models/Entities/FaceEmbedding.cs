namespace FaceRecognitionAPI.Models.Entities
{
    public class FaceEmbedding
    {
        public int User_ID { get; set; }
        public string Embedding { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

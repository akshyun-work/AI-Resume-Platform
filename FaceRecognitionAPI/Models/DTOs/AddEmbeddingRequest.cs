namespace FaceRecognitionAPI.Models.DTOs
{
    public class AddEmbeddingRequest
    {
        public int UserId { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}

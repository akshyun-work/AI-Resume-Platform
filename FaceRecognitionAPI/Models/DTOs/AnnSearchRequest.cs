namespace FaceRecognitionAPI.Models.DTOs
{
    public class AnnSearchRequest
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}

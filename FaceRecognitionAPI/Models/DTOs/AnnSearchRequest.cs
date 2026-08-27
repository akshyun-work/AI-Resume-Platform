using System.Text.Json.Serialization;

namespace FaceRecognitionAPI.Models.DTOs
{
    public class AnnSearchRequest
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
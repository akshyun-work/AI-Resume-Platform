using System.Text.Json.Serialization;

namespace FaceRecognitionAPI.Models.DTOs
{
    public class IndexStatusResponse
    {
        [JsonPropertyName("total_embeddings")]
        public int TotalEmbeddings { get; set; }
    }
}
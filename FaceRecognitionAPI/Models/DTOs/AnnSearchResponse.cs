using System.Text.Json.Serialization;

namespace FaceRecognitionAPI.Models.DTOs
{
    public class AnnSearchResponse
    {
        [JsonPropertyName("user_ids")]
        public List<int> UserIds { get; set; } = new();
    }
}
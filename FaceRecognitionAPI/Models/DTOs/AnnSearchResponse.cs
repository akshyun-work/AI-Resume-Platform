using System.Text.Json.Serialization;

namespace FaceRecognitionAPI.Models.DTOs
{
    public class AnnSearchResponse
    {
        [JsonPropertyName("candidate_ids")]
        public List<Guid> CandidateIds { get; set; } = new();
    }
}
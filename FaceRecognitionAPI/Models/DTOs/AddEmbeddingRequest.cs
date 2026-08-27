namespace FaceRecognitionAPI.Models.DTOs
{
    public class AddEmbeddingRequest
    {
        public Guid CandidateId { get; set; }

        public List<float> Embedding { get; set; } =
            new();
    }
}
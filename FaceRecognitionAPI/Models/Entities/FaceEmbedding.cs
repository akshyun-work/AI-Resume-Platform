using ResumeAnalysis.Api.Entities;

namespace FaceRecognitionAPI.Models.Entities
{
    public class FaceEmbedding
    {
        public Guid CandidateId { get; set; }

        public string Embedding { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Candidate Candidate { get; set; } = null!;
    }
}
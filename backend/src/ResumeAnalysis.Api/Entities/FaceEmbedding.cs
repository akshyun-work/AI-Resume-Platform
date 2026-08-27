namespace ResumeAnalysis.Api.Entities;

public class FaceEmbedding
{
	public Guid CandidateId { get; set; }

	public Candidate Candidate { get; set; } = null!;

	public string Embedding { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
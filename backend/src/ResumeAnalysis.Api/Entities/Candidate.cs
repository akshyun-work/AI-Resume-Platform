namespace ResumeAnalysis.Api.Entities;

public class Candidate
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    public FaceEmbedding? FaceEmbedding { get; set; }
}

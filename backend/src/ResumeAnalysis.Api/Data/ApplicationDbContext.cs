using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Entities;

namespace ResumeAnalysis.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<AtsAnalysis> AtsAnalyses => Set<AtsAnalysis>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<FaceEmbedding> FaceEmbeddings => Set<FaceEmbedding>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Candidate>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Resume>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Candidate).WithMany(c => c.Resumes).HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
            e.HasIndex(x => new { x.CandidateId, x.VersionNumber }).IsUnique();
            e.HasIndex(x => new { x.CandidateId, x.IsLatest });
        });

        modelBuilder.Entity<AtsAnalysis>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Resume).WithMany(r => r.Analyses).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Candidate).WithMany().HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.ResumeId);
            e.HasIndex(x => x.CandidateId);
        });

        modelBuilder.Entity<Job>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Company).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).IsRequired();
            e.HasIndex(x => x.IsActive);
        });

        modelBuilder.Entity<JobApplication>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Candidate).WithMany(c => c.Applications).HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Job).WithMany(j => j.Applications).HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Resume).WithMany().HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.CandidateId, x.JobId }).IsUnique();
        });

        modelBuilder.Entity<MatchResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Candidate).WithMany().HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Resume).WithMany(r => r.MatchResults).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Job).WithMany(j => j.MatchResults).HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CandidateId, x.JobId, x.ResumeId });
        });

        modelBuilder.Entity<ChatSession>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Candidate).WithMany(c => c.ChatSessions).HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Title).HasMaxLength(200);
        });

        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ChatSession).WithMany(s => s.Messages).HasForeignKey(x => x.ChatSessionId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Content).IsRequired();
            e.HasIndex(x => x.ChatSessionId);
        });

        modelBuilder.Entity<FaceEmbedding>(e =>
        {
            e.HasKey(x => x.CandidateId);

            e.HasOne(x => x.Candidate)
                .WithOne(c => c.FaceEmbedding)
                .HasForeignKey<FaceEmbedding>(x => x.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.Embedding)
                .IsRequired();

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<PasswordResetOtp>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Otp).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.Email);
            e.HasIndex(x => new { x.Email, x.IsUsed, x.ExpiresAt });
        });
    }
}

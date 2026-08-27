using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Entities;

namespace FaceRecognitionAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Candidate> Candidates { get; set; }

        public DbSet<FaceEmbedding> FaceEmbeddings { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Candidate>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.ToTable("Candidates");

                entity.Property(c => c.Id)
                    .ValueGeneratedNever();

                entity.Property(c => c.Email)
                    .IsRequired()
                    .HasMaxLength(256);
            });

            modelBuilder.Entity<FaceEmbedding>(entity =>
            {
                entity.HasKey(f => f.CandidateId);

                entity.ToTable("FaceEmbeddings");

                entity.Property(f => f.CandidateId)
                    .ValueGeneratedNever();

                entity.Property(f => f.Embedding)
                    .IsRequired();

                entity.Property(f => f.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(f => f.Candidate)
                    .WithOne(c => c.FaceEmbedding)
                    .HasForeignKey<FaceEmbedding>(
                        f => f.CandidateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
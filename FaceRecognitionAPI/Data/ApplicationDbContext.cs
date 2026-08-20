using FaceRecognitionAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FaceRecognitionAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ── Table 1: Users ────────────────────────────────────────────────────
        public DbSet<User> Users { get; set; }

        // ── Table 2: Face recognition mapping ────────────────────────────────
        public DbSet<FaceEmbedding> FaceEmbeddings { get; set; }

        // ── Table 3: Resume mapping ───────────────────────────────────────────
        public DbSet<Resume> Resumes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Users ─────────────────────────────────────────────────────────
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.Property(u => u.UserId).ValueGeneratedOnAdd();

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(256);

                // Enforce unique emails
                entity.HasIndex(u => u.Email).IsUnique();

                entity.Property(u => u.PasswordHash)
                      .IsRequired();

                entity.Property(u => u.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
            });

            // ── FaceEmbeddings (one-to-one with Users) ────────────────────────
            modelBuilder.Entity<FaceEmbedding>(entity =>
            {
                // UserId is both PK and FK
                entity.HasKey(f => f.UserId);

                entity.Property(f => f.Embedding)
                      .IsRequired();

                entity.Property(f => f.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // One User → One FaceEmbedding
                entity.HasOne(f => f.User)
                      .WithOne(u => u.FaceEmbedding)
                      .HasForeignKey<FaceEmbedding>(f => f.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Resumes (one-to-many with Users) ─────────────────────────────
            modelBuilder.Entity<Resume>(entity =>
            {
                entity.HasKey(r => r.ResumeId);
                entity.Property(r => r.ResumeId).ValueGeneratedOnAdd();

                entity.Property(r => r.FileName)
                      .IsRequired()
                      .HasMaxLength(512);

                entity.Property(r => r.FilePath)
                      .IsRequired()
                      .HasMaxLength(1024);

                entity.Property(r => r.UploadedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // One User → Many Resumes
                entity.HasOne(r => r.User)
                      .WithMany(u => u.Resumes)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}


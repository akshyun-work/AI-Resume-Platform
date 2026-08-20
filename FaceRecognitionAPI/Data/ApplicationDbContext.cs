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

        public DbSet<FaceEmbedding> FaceEmbeddings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FaceEmbedding>(entity =>
            {
                entity.HasKey(x => x.User_ID);

                entity.Property(x => x.Embedding)
                      .IsRequired();
            });
        }
    }
}

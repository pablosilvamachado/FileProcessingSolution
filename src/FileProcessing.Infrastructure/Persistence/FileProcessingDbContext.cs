using Microsoft.EntityFrameworkCore;
using FileProcessing.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FileProcessing.Infrastructure.Persistence
{
    public class FileProcessingDbContext : DbContext
    {
        public FileProcessingDbContext(DbContextOptions<FileProcessingDbContext> options) : base(options) { }

        public DbSet<FileRecord> Files { get; set; } = null!;

        public DbSet<ProcessedMessage> ProcessedMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTime, DateTime>(
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                            )
                        );
                    }

                    if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTime?, DateTime?>(
                                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
                                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v
                            )
                        );
                    }
                }

                modelBuilder.Entity<ProcessedMessage>()
                    .ToTable("ProcessedMessages")
                    .HasKey(p => p.MessageId);
            }
            
            modelBuilder.Entity<FileRecord>()
                .ToTable("Files");

            base.OnModelCreating(modelBuilder);
        }
    }
}

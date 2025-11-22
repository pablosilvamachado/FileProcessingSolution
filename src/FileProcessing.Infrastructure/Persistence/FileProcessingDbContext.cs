using Microsoft.EntityFrameworkCore;
using FileProcessing.Domain.Entities;

namespace FileProcessing.Infrastructure.Persistence
{
    public class FileProcessingDbContext : DbContext
    {
        public FileProcessingDbContext(DbContextOptions<FileProcessingDbContext> options) : base(options) { }

        public DbSet<FileRecord> Files { get; set; } = null!;
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FileProcessing.Application.Interfaces;
using FileProcessing.Domain.Entities;
using FileProcessing.Infrastructure.Persistence;

namespace FileProcessing.Infrastructure.Repositories
{
    public class FileRecordRepository : IFileRecordRepository
    {
        private readonly FileProcessingDbContext _db;

        public FileRecordRepository(FileProcessingDbContext db) => _db = db;

        public async Task AddAsync(FileRecord file, CancellationToken cancellationToken = default)
        {
            _db.Files.Add(file);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<FileRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Files.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        public async Task<bool> TryMarkProcessingAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Files"" SET ""Status"" = {"Processing"} 
                WHERE ""Id"" = {id} AND ""Status"" = {"Pending"}
            ", cancellationToken);
            return rows > 0;
        }

        public async Task UpdateAsync(FileRecord file, CancellationToken cancellationToken = default)
        {
            _db.Files.Update(file);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using FileProcessing.Domain.Entities;

namespace FileProcessing.Application.Interfaces
{
    public interface IFileRecordRepository
    {
        Task AddAsync(FileRecord file, CancellationToken cancellationToken = default);
        Task<FileRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> TryMarkProcessingAsync(Guid id, CancellationToken cancellationToken = default);
        Task UpdateAsync(FileRecord file, CancellationToken cancellationToken = default);
    }
}

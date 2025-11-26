using FileProcessing.Application.Interfaces;
using FileProcessing.Domain.Entities;
using FileProcessing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FileProcessing.Infrastructure.Repositories
{
    public class ProcessedMessageRepository : IProcessedMessageRepository
    {
        private readonly FileProcessingDbContext _db;
        public ProcessedMessageRepository(FileProcessingDbContext db) => _db = db;

        public async Task<bool> HasProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            var exists = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT 1 FROM ""ProcessedMessages"" WHERE ""MessageId"" = {messageId} LIMIT 1
            ", cancellationToken);

            return exists > 0; 
        }

        public async Task MarkProcessedAsync(Guid messageId, string messageType, CancellationToken cancellationToken = default)
        {
            var entry = new ProcessedMessage
            {
                MessageId = messageId,
                MessageType = messageType ?? "Unknown",
                ReceivedAt = DateTime.UtcNow
            };
            _db.Set<ProcessedMessage>().Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

namespace FileProcessing.Application.Interfaces
{
    public interface IProcessedMessageRepository
    {
        Task<bool> HasProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
        Task MarkProcessedAsync(Guid messageId, string messageType, CancellationToken cancellationToken = default);
    }
}

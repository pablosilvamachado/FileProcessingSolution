using FileProcessing.Contracts.Messaging;

namespace FileProcessing.Application.Interfaces
{
    public interface IMessageProducerService
    {
        Task PublishFileUploadedAsync(FileUploadedMessage message, CancellationToken cancellationToken = default);
    }
}

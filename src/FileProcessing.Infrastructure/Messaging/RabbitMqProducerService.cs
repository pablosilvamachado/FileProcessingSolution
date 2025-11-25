using FileProcessing.Application.Interfaces;
using MassTransit;


namespace FileProcessing.Infrastructure.Messaging
{
    public class RabbitMqProducerService : IMessageProducerService
    {
        private readonly IPublishEndpoint _publish;
        public RabbitMqProducerService(IPublishEndpoint publish) => _publish = publish;
        public async Task PublishFileUploadedAsync(FileUploadedMessage message, CancellationToken cancellationToken = default)
        {
            await _publish.Publish(message, cancellationToken);
        }
    }
}

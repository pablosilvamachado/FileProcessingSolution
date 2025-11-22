using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using FileProcessing.Application.Interfaces;
using FileProcessing.Infrastructure.Messaging;

namespace FileProcessing.Api.Services
{
    public class MessageProducerService : IMessageProducerService
    {
        private readonly IPublishEndpoint _publish;
        public MessageProducerService(IPublishEndpoint publish) => _publish = publish;

        public async Task PublishFileUploadedAsync(object message, CancellationToken cancellationToken = default)
        {
            // Publish using MassTransit
            await _publish.Publish(message, cancellationToken);
        }
    }
}

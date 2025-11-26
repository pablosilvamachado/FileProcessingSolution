using MassTransit;
using FileProcessing.Application.Interfaces;
using FileProcessing.Contracts.Messaging;


namespace FileProcessing.Api.Services
{
    public class MessageProducerService : IMessageProducerService
    {
        private readonly ISendEndpointProvider _send;

        public MessageProducerService(ISendEndpointProvider send) => _send = send;

        public async Task PublishFileUploadedAsync(FileUploadedMessage message, CancellationToken cancellationToken = default)
        {
            var endpoint = await _send.GetSendEndpoint(new Uri("queue:upload_queue"));
            await endpoint.Send(message, cancellationToken);
        }
    }
}

using FileProcessing.Application.Interfaces;
using FileProcessing.Contracts.Messaging;
using MassTransit;
using Serilog;


namespace FileProcessing.Infrastructure.Messaging
{
    public class RabbitMqProducerService : IMessageProducerService
    {
        private readonly IPublishEndpoint _publish;
        public RabbitMqProducerService(IPublishEndpoint publish) => _publish = publish;
        public async Task PublishFileUploadedAsync(FileUploadedMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                await _publish.Publish(message, cancellationToken);
                Log.Information("Publicando mensagem na fila: {@Message}", message);
            }
            catch (Exception)
            {
                Log.Information("Erro ao publicar: {@Message}", message);
                throw;
            }
            
        }
    }
}

using System.Threading;
using System.Threading.Tasks;

namespace FileProcessing.Application.Interfaces
{
    public interface IMessageProducerService
    {
        Task PublishFileUploadedAsync(object message, CancellationToken cancellationToken = default);
    }
}

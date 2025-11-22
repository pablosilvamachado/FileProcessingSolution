using MassTransit;
using System.Threading.Tasks;

namespace FileProcessing.Worker.Consumers
{
    public class PlaceholderConsumer : IConsumer<object>
    {
        public Task Consume(ConsumeContext<object> context)
        {
            return Task.CompletedTask;
        }
    }
}

using MassTransit;
using Microsoft.Extensions.Logging;
using Polly;
using FileProcessing.Application.Interfaces;
using FileProcessing.Infrastructure.Messaging;

namespace FileProcessing.Worker.Consumers;

public class FileUploadedConsumer : IConsumer<FileUploadedMessage>
{
    private readonly IFileRecordRepository _repo;
    private readonly IFileStorageService _storage;
    private readonly ILogger<FileUploadedConsumer> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public FileUploadedConsumer(IFileRecordRepository repo, IFileStorageService storage, ILogger<FileUploadedConsumer> logger)
    {
        _repo = repo;
        _storage = storage;
        _logger = logger;

        _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        });
    }

    public async Task Consume(ConsumeContext<FileUploadedMessage> context)
    {
        var msg = context.Message;
        var fileId = msg.File.FileId;

        // Idempotency: try to mark as Processing
        var locked = await _repo.TryMarkProcessingAsync(fileId);
        if (!locked)
        {
            _logger.LogInformation("File {FileId} already processing or processed - skipping", fileId);
            return;
        }

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using var stream = await _storage.DownloadTempAsync(msg.File.TempPath);
                // Validate size/type here if needed
                var finalPath = await _storage.MoveTempToFinalAsync(msg.File.TempPath, fileId.ToString());
                var entity = await _repo.GetAsync(fileId);
                if (entity == null) throw new InvalidOperationException("File meta missing");
                entity.MarkCompleted(finalPath);
                await _repo.UpdateAsync(entity);
            });

            _logger.LogInformation("Processed {FileId} successfully", fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing {FileId}", fileId);

            var entity = await _repo.GetAsync(fileId);
            if (entity != null)
            {
                entity.IncrementRetry();
                entity.MarkFailed(ex.Message);
                await _repo.UpdateAsync(entity);
            }

            // Optionally publish to retry queue manually:
            // await context.Publish(context.Message, sendCtx => sendCtx.DestinationAddress = new Uri("exchange:upload_queue_retry"));

            // Rethrow so MassTransit/RabbitMQ handles retry and DLQ
            throw;
        }
    }
}

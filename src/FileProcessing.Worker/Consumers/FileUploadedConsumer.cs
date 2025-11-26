using FileProcessing.Application.Interfaces;
using FileProcessing.Contracts.Messaging;
using MassTransit;
using Polly;
using Serilog;
using Microsoft.Extensions.Logging;

namespace FileProcessing.Worker.Consumers;

public class FileUploadedConsumer : IConsumer<FileUploadedMessage>
{
    private readonly IFileRecordRepository _repo;
    private readonly IFileStorageService _storage;
    private readonly IProcessedMessageRepository _processedRepo;
    private readonly ILogger<FileUploadedConsumer> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public FileUploadedConsumer(
        IFileRecordRepository repo,
        IFileStorageService storage,
        IProcessedMessageRepository processedRepo,
        ILogger<FileUploadedConsumer> logger)
    {
        _repo = repo;
        _storage = storage;
        _processedRepo = processedRepo;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) },
                (exception, time, retryCount, context) =>
                {
                    Log.Warning(exception,
                        "Retry {RetryCount} after {DelaySeconds}s due to: {Message}",
                        retryCount, time.TotalSeconds, exception.Message);
                });
    }

    public async Task Consume(ConsumeContext<FileUploadedMessage> context)
    {
        var msg = context.Message;
        var fileId = msg.File.FileId;
        var messageType = msg.File.MesageType ?? nameof(FileUploadedMessage);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = context.MessageId?.ToString() ?? "-",
            ["FileId"] = fileId.ToString(),
            ["CorrelationId"] = context.CorrelationId?.ToString() ?? "-",
            ["ConversationId"] = context.ConversationId?.ToString() ?? "-",
            ["MessageType"] = messageType,
            ["Consumer"] = nameof(FileUploadedConsumer)
        }))
        {
            _logger.LogInformation("Received message for file processing");

            
            if (await _processedRepo.HasProcessedAsync(fileId))
            {
                _logger.LogInformation("Skipping. File {FileId} already processed previously.", fileId);
                return;
            }

            var locked = await _repo.TryMarkProcessingAsync(fileId);
            if (!locked)
            {
                _logger.LogWarning("File {FileId} already being processed by another worker — skipping.", fileId);
                return;
            }

            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    _logger.LogInformation(
                        "Starting file processing. TempPath={TempPath}, Attempting download...",
                        msg.File.TempPath);
          
                    using var stream = await _storage.DownloadTempAsync(msg.File.TempPath);

                    _logger.LogInformation("Download OK. Moving file to final storage...");

                    var finalPath = await _storage.MoveTempToFinalAsync(msg.File.TempPath, fileId.ToString());
                    _logger.LogInformation("File moved to final path: {FinalPath}", finalPath);

                    var entity = await _repo.GetAsync(fileId);
                    if (entity == null)
                    {
                        throw new InvalidOperationException($"Metadata not found for FileId={fileId}");
                    }

                    entity.MarkCompleted(finalPath);
                    await _repo.UpdateAsync(entity);

                    _logger.LogInformation("Database updated successfully for FileId={FileId}", fileId);
                });
                
                await _processedRepo.MarkProcessedAsync(fileId, messageType);

                _logger.LogInformation("File {FileId} processed successfully", fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file {FileId}. Applying failure state.", fileId);

                var entity = await _repo.GetAsync(fileId);
                if (entity != null)
                {
                    entity.IncrementRetry();
                    entity.MarkFailed(ex.Message);
                    await _repo.UpdateAsync(entity);
                }

                throw; 
            }
        }
    }
}

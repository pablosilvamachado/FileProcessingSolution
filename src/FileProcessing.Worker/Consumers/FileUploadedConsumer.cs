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
                new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20) },
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
            _logger.LogInformation($"Consumer - Received message. ID {fileId}");

            if (await _processedRepo.HasProcessedAsync(fileId))
                return;
            
            if (!await _repo.TryMarkProcessingAsync(fileId))
                return;

            try
            {

                using var stream = await _storage.DownloadTempAsync(msg.File.TempPath);
                _logger.LogInformation($"Consumer - Reading File in Temp Path: {fileId}");

                var finalPath = await _storage.MoveTempToFinalAsync(
                    msg.File.TempPath,
                    fileId.ToString()
                ) ;
                
                var entity = await _repo.GetAsync(fileId)
                    ?? throw new InvalidOperationException($"Metadata not found for {fileId}");

                entity.MarkCompleted(finalPath);
                _logger.LogInformation($"Consumer - Moving File to Final Path: {fileId}");

                await _repo.UpdateAsync(entity);
                _logger.LogInformation($"Consumer - Updating DataBase Registry: {fileId}");

                await _processedRepo.MarkProcessedAsync(fileId, nameof(FileUploadedMessage));
                _logger.LogInformation($"Consumer - File Processed: {fileId}");
            }
            catch (Exception ex)
            {
                if (ex.InnerException.Message.Contains("Metadata not found for"))
                    _logger.LogError($"Consumer - Metadata not found for {fileId}");

                _logger.LogError(ex, "Error on processing file {FileId}", fileId);

                var entity = await _repo.GetAsync(fileId);
                if (entity != null)
                {
                    _logger.LogError($"Consumer - Retry Process for {fileId}");
                    entity.IncrementRetry();
                    entity.MarkFailed(ex.Message);
                    await _repo.UpdateAsync(entity);
                }

                throw; 
            }
        }
    }
}


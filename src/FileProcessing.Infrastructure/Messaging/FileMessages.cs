using System;

namespace FileProcessing.Infrastructure.Messaging
{
    public record FileInfoDto(Guid FileId, string FileName, string ContentType, long Size, string TempPath);
    public record MetaDto(string Uploader, int Attempt);
    public record FileUploadedMessage(Guid MessageId, string MessageType, DateTime CreatedAt, Guid CorrelationId, FileInfoDto File, MetaDto Meta);
    public record FileProcessedMessage(Guid MessageId, string MessageType, DateTime CreatedAt, Guid CorrelationId, Guid FileId, string Status, object? Result);
}

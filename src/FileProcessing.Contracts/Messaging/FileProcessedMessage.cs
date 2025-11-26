namespace FileProcessing.Contracts.Messaging
{
    public record FileProcessedMessage(Guid MessageId, string MessageType, DateTime CreatedAt, Guid CorrelationId, Guid FileId, string Status, object? Result);
}

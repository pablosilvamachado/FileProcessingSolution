using System;

namespace FileProcessing.Domain.Entities
{
    public class ProcessedMessage
    {
        public Guid MessageId { get; set; }
        public string MessageType { get; set; } = null!;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}

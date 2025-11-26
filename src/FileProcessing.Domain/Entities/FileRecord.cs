using System;
using System.ComponentModel.DataAnnotations;

namespace FileProcessing.Domain.Entities
{
    public class FileRecord
    {
        public Guid Id { get; private set; }
        public string FileName { get; private set; } = null!;
        public string ContentType { get; private set; } = null!;
        public long Size { get; private set; }
        public string TempPath { get; private set; } = null!;
        public string? FinalPath { get; private set; }
        public string Status { get; private set; } = "Pending";
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; private set; }
        public string? ErrorMessage { get; private set; }
        public int RetryCount { get; private set; }

        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Failed = "Failed";
        public const string Completed = "Completed";

        private FileRecord() { }

        public FileRecord(Guid id, string fileName, string contentType, long size, string tempPath)
        {
            Id = id;
            FileName = fileName;
            ContentType = contentType;
            Size = size;
            TempPath = tempPath;
            Status = "Pending";
            RetryCount = 0;
        }
        public void MarkProcessing()
        {
            Status = "Processing";
        }

        public void MarkCompleted(string finalPath)
        {
            Status = "Completed";
            FinalPath = finalPath;
            ProcessedAt = DateTime.UtcNow;
        }

        public void MarkFailed(string error)
        {
            Status = "Failed";
            ErrorMessage = error;
            ProcessedAt = DateTime.UtcNow;
        }

        public void IncrementRetry()
        {
            RetryCount++;
        }       
    }
}

namespace FileProcessing.Contracts.Messaging
{
    public class FileUploadedMessage
    {
        public FileUploadedPayload File { get; set; } = default!;
    }

    public class FileUploadedPayload
    {
        public Guid FileId { get; set; }
        public string TempPath { get; set; } = default!;

        public string MesageType { get; set; } = string .Empty;
    }

}

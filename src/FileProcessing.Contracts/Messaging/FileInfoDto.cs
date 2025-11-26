namespace FileProcessing.Contracts.Messaging
{
    public record FileInfoDto(Guid FileId, string FileName, string ContentType, long Size, string TempPath);    
}

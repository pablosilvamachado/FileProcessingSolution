using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileProcessing.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadTempAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
        Task<Stream> DownloadTempAsync(string path, CancellationToken cancellationToken = default);
        Task<string> MoveTempToFinalAsync(string tempPath, string finalFileName, CancellationToken cancellationToken = default);
        Task DeleteTempAsync(string tempPath, CancellationToken cancellationToken = default);
        Task<bool> CheckHealthAsync();
    }
}

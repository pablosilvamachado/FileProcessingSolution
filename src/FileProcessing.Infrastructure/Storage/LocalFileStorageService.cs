using FileProcessing.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FileProcessing.Infrastructure.Storage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _baseTemp;
        private readonly string _baseFinal;

        public LocalFileStorageService(IConfiguration configuration)
        {
            var basePath = configuration.GetValue<string>("Storage:Local:BasePath") ?? Path.GetTempPath();
            _baseTemp = Path.Combine(basePath, "temp");
            _baseFinal = Path.Combine(basePath, "final");
            Directory.CreateDirectory(_baseTemp);
            Directory.CreateDirectory(_baseFinal);
        }

        public async Task<string> UploadTempAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(_baseTemp, fileName);
            using var fs = File.Create(path);
            await stream.CopyToAsync(fs, cancellationToken);
            return path;
        }

        public Task<Stream> DownloadTempAsync(string path, CancellationToken cancellationToken = default)
        {
            Stream s = File.OpenRead(path);
            return Task.FromResult(s);
        }

        public Task DeleteTempAsync(string tempPath, CancellationToken cancellationToken = default)
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return Task.CompletedTask;
        }

        public Task<string> MoveTempToFinalAsync(string tempPath, string finalFileName, CancellationToken cancellationToken = default)
        {
            var finalPath = Path.Combine(_baseFinal, finalFileName + Path.GetExtension(tempPath));
            if (File.Exists(finalPath)) return Task.FromResult(finalPath); // idempotency
            File.Move(tempPath, finalPath);
            return Task.FromResult(finalPath);
        }
    }
}

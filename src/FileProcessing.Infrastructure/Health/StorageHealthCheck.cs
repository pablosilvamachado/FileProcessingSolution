using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileProcessing.Infrastructure.Health
{
    public class StorageHealthCheck : IHealthCheck
    {
        private readonly string _basePath;
        public StorageHealthCheck(IConfiguration configuration)
        {
            // procure a configuração real (ex: FileStorage:RootTempPath)
            _basePath = configuration.GetValue<string>("FileStorage:RootTempPath") ?? "/app/uploads";
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(_basePath))
                    return Task.FromResult(HealthCheckResult.Unhealthy($"Path {_basePath} does not exist"));

                var testFile = Path.Combine(_basePath, "hc_test.tmp");
                File.WriteAllText(testFile, "ok");
                File.Delete(testFile);

                return Task.FromResult(HealthCheckResult.Healthy("Storage OK"));
            }
            catch (System.Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Storage check failed", ex));
            }
        }
    }
}

using FileProcessing.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FileProcessing.Infrastructure.Health;

public class StorageHealthCheck : IHealthCheck
{
    private readonly IFileStorageService _storage;

    public StorageHealthCheck(IFileStorageService storage)
    {
        _storage = storage;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ok = await _storage.CheckHealthAsync();
            return ok
                ? HealthCheckResult.Healthy("Storage OK")
                : HealthCheckResult.Unhealthy("Storage failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Storage exception", ex);
        }
    }
}

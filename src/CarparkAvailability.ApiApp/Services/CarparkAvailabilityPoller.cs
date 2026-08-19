using Microsoft.Extensions.Options;

namespace CarparkAvailability.ApiApp.Services;

public sealed class CarparkAvailabilityPoller(
    IDataGovSgCarparkClient client,
    ICarparkAvailabilitySnapshotStore snapshotStore,
    ILogger<CarparkAvailabilityPoller> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PollOnceAsync(stoppingToken);

        using PeriodicTimer timer = new(PollInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollOnceAsync(stoppingToken);
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            snapshotStore.Update(await client.FetchLatestAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to refresh car park availability. The last-known-good snapshot will be retained.");
        }
    }
}

using CarparkAvailability.ApiApp.Models;

namespace CarparkAvailability.ApiApp.Services;

public interface ICarparkService
{
    NearbyCarparksResponse GetNearby(double latitude, double longitude, double radiusMetres);
    CarParkDetailResponse? GetCarPark(string carParkNo);
}

public sealed class CarparkService(
    IHdbCarparkRepository repository,
    ICarparkAvailabilitySnapshotStore snapshotStore,
    TimeProvider timeProvider) : ICarparkService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    public NearbyCarparksResponse GetNearby(double latitude, double longitude, double radiusMetres)
    {
        LiveCarparkSnapshot snapshot = snapshotStore.GetSnapshot() ?? throw new NoCarparkSnapshotException();
        long cacheAge = GetCacheAgeInSeconds(snapshot);
        bool usingLastKnownGood = cacheAge > PollInterval.TotalSeconds;

        IReadOnlyList<NearbyCarparkResponse> carParks = CarparkSearchLogic.GetNearbyCarParks(
            repository.CarParks.Values,
            snapshot.CarParks,
            latitude,
            longitude,
            radiusMetres,
            snapshot.SnapshotTime);

        return new NearbyCarparksResponse(snapshot.SnapshotTime, cacheAge, usingLastKnownGood, carParks);
    }

    public CarParkDetailResponse? GetCarPark(string carParkNo)
    {
        if (!repository.CarParks.TryGetValue(carParkNo, out StaticCarpark? carPark))
        {
            return null;
        }

        LiveCarparkSnapshot snapshot = snapshotStore.GetSnapshot() ?? throw new NoCarparkSnapshotException();
        snapshot.CarParks.TryGetValue(carParkNo, out LiveCarparkAvailability? liveAvailability);
        long cacheAge = GetCacheAgeInSeconds(snapshot);
        bool usingLastKnownGood = cacheAge > PollInterval.TotalSeconds;

        return CarparkSearchLogic.ToDetailResponse(carPark, liveAvailability, snapshot.SnapshotTime, cacheAge, usingLastKnownGood);
    }

    private long GetCacheAgeInSeconds(LiveCarparkSnapshot snapshot) =>
        Math.Max(0, (long)Math.Floor((timeProvider.GetUtcNow() - snapshot.FetchedAt).TotalSeconds));
}

public sealed class NoCarparkSnapshotException : Exception
{
    public NoCarparkSnapshotException() : base("Live car park availability is not available yet.")
    {
    }
}

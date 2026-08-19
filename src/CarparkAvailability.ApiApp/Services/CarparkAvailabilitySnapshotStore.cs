using CarparkAvailability.ApiApp.Models;

namespace CarparkAvailability.ApiApp.Services;

public interface ICarparkAvailabilitySnapshotStore
{
    LiveCarparkSnapshot? GetSnapshot();
    void Update(LiveCarparkSnapshot snapshot);
}

public sealed class CarparkAvailabilitySnapshotStore : ICarparkAvailabilitySnapshotStore
{
    private LiveCarparkSnapshot? currentSnapshot;

    public LiveCarparkSnapshot? GetSnapshot() => Volatile.Read(ref currentSnapshot);

    public void Update(LiveCarparkSnapshot snapshot) => Volatile.Write(ref currentSnapshot, snapshot);
}

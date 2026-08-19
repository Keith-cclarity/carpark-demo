namespace CarparkAvailability.ApiApp.Models;

public sealed record LotAvailability(string LotType, int TotalLots, int LotsAvailable);

public sealed record LiveCarparkAvailability(
    string CarParkNo,
    DateTimeOffset? UpdateDatetime,
    IReadOnlyList<LotAvailability> Lots);

public sealed record LiveCarparkSnapshot(
    DateTimeOffset SnapshotTime,
    DateTimeOffset FetchedAt,
    IReadOnlyDictionary<string, LiveCarparkAvailability> CarParks);

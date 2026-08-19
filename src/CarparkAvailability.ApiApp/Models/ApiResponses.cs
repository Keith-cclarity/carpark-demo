namespace CarparkAvailability.ApiApp.Models;

public sealed record NearbyCarparksResponse(
    DateTimeOffset SnapshotTime,
    long CacheAge,
    bool UsingLastKnownGood,
    IReadOnlyList<NearbyCarparkResponse> CarParks);

public sealed record NearbyCarparkResponse(
    string CarParkNo,
    string Address,
    double Latitude,
    double Longitude,
    int DistanceMetres,
    string CarParkType,
    string ParkingSystem,
    string ShortTermParking,
    string FreeParking,
    bool NightParking,
    int? CarParkDecks,
    double? GantryHeight,
    bool CarParkBasement,
    bool StaticDataAvailable,
    DateTimeOffset? UpdateDatetime,
    bool IsStale,
    IReadOnlyList<LotAvailability> Lots);

public sealed record CarParkDetailResponse(
    DateTimeOffset SnapshotTime,
    long CacheAge,
    bool UsingLastKnownGood,
    string CarParkNo,
    string Address,
    double Latitude,
    double Longitude,
    string CarParkType,
    string ParkingSystem,
    string ShortTermParking,
    string FreeParking,
    bool NightParking,
    int? CarParkDecks,
    double? GantryHeight,
    bool CarParkBasement,
    bool StaticDataAvailable,
    DateTimeOffset? UpdateDatetime,
    bool IsStale,
    IReadOnlyList<LotAvailability> Lots);

public sealed record JoinedCarpark(StaticCarpark StaticCarpark, LiveCarparkAvailability? LiveAvailability);

public sealed record CarparkJoinSummary(
    IReadOnlyList<JoinedCarpark> Matched,
    IReadOnlyList<StaticCarpark> StaticOnly,
    IReadOnlyList<LiveCarparkAvailability> LiveOnly);

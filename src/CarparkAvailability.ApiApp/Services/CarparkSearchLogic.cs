using CarparkAvailability.ApiApp.Models;

namespace CarparkAvailability.ApiApp.Services;

public static class CarparkSearchLogic
{
    private const double EarthRadiusMetres = 6_371_000d;

    public static CarparkJoinSummary JoinByCarParkNumber(
        IEnumerable<StaticCarpark> staticCarParks,
        IEnumerable<LiveCarparkAvailability> liveCarParks)
    {
        Dictionary<string, StaticCarpark> staticLookup = staticCarParks.ToDictionary(carPark => carPark.CarParkNo, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, LiveCarparkAvailability> liveLookup = liveCarParks.ToDictionary(carPark => carPark.CarParkNo, StringComparer.OrdinalIgnoreCase);

        List<JoinedCarpark> matched = [];
        List<StaticCarpark> staticOnly = [];
        List<LiveCarparkAvailability> liveOnly = [];

        foreach ((string key, StaticCarpark staticCarPark) in staticLookup)
        {
            if (liveLookup.TryGetValue(key, out LiveCarparkAvailability? liveAvailability))
            {
                matched.Add(new JoinedCarpark(staticCarPark, liveAvailability));
                continue;
            }

            staticOnly.Add(staticCarPark);
        }

        foreach ((string key, LiveCarparkAvailability liveCarPark) in liveLookup)
        {
            if (!staticLookup.ContainsKey(key))
            {
                liveOnly.Add(liveCarPark);
            }
        }

        return new CarparkJoinSummary(matched, staticOnly, liveOnly);
    }

    public static IReadOnlyList<NearbyCarparkResponse> GetNearbyCarParks(
        IEnumerable<StaticCarpark> staticCarParks,
        IReadOnlyDictionary<string, LiveCarparkAvailability> liveCarParks,
        double latitude,
        double longitude,
        double radiusMetres,
        DateTimeOffset snapshotTime)
    {
        List<NearbyCarparkResponse> results = [];

        foreach (StaticCarpark carPark in staticCarParks)
        {
            double distance = CalculateDistanceMetres(latitude, longitude, carPark.Latitude, carPark.Longitude);
            if (distance > radiusMetres)
            {
                continue;
            }

            liveCarParks.TryGetValue(carPark.CarParkNo, out LiveCarparkAvailability? liveAvailability);
            results.Add(ToNearbyResponse(carPark, liveAvailability, snapshotTime, distance));
        }

        return results
            .OrderBy(result => result.DistanceMetres)
            .ThenBy(result => result.CarParkNo, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static CarParkDetailResponse ToDetailResponse(
        StaticCarpark carPark,
        LiveCarparkAvailability? liveAvailability,
        DateTimeOffset snapshotTime,
        long cacheAge,
        bool usingLastKnownGood) =>
        new(
            snapshotTime,
            cacheAge,
            usingLastKnownGood,
            carPark.CarParkNo,
            carPark.Address,
            carPark.Latitude,
            carPark.Longitude,
            carPark.CarParkType,
            carPark.ParkingSystem,
            carPark.ShortTermParking,
            carPark.FreeParking,
            carPark.NightParking,
            carPark.CarParkDecks,
            carPark.GantryHeight,
            carPark.CarParkBasement,
            carPark.StaticDataAvailable,
            liveAvailability?.UpdateDatetime,
            IsStale(snapshotTime, liveAvailability?.UpdateDatetime),
            liveAvailability?.Lots ?? []);

    public static bool IsStale(DateTimeOffset snapshotTime, DateTimeOffset? updateDatetime) =>
        updateDatetime.HasValue && snapshotTime - updateDatetime.Value > TimeSpan.FromMinutes(5);

    public static double CalculateDistanceMetres(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        double latitudeRadians1 = DegreesToRadians(latitude1);
        double latitudeRadians2 = DegreesToRadians(latitude2);
        double deltaLatitude = DegreesToRadians(latitude2 - latitude1);
        double deltaLongitude = DegreesToRadians(longitude2 - longitude1);

        double a = Math.Sin(deltaLatitude / 2d) * Math.Sin(deltaLatitude / 2d)
            + (Math.Cos(latitudeRadians1) * Math.Cos(latitudeRadians2) * Math.Sin(deltaLongitude / 2d) * Math.Sin(deltaLongitude / 2d));
        double c = 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
        return EarthRadiusMetres * c;
    }

    private static NearbyCarparkResponse ToNearbyResponse(
        StaticCarpark carPark,
        LiveCarparkAvailability? liveAvailability,
        DateTimeOffset snapshotTime,
        double distance) =>
        new(
            carPark.CarParkNo,
            carPark.Address,
            carPark.Latitude,
            carPark.Longitude,
            (int)Math.Round(distance, MidpointRounding.AwayFromZero),
            carPark.CarParkType,
            carPark.ParkingSystem,
            carPark.ShortTermParking,
            carPark.FreeParking,
            carPark.NightParking,
            carPark.CarParkDecks,
            carPark.GantryHeight,
            carPark.CarParkBasement,
            carPark.StaticDataAvailable,
            liveAvailability?.UpdateDatetime,
            IsStale(snapshotTime, liveAvailability?.UpdateDatetime),
            liveAvailability?.Lots ?? []);

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}

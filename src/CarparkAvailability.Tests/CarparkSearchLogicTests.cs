using CarparkAvailability.ApiApp.Models;
using CarparkAvailability.ApiApp.Services;

namespace CarparkAvailability.Tests;

public sealed class CarparkSearchLogicTests
{
    [Fact]
    public void JoinByCarParkNumber_reports_matched_and_unmatched_entries()
    {
        StaticCarpark matchedStatic = CreateStaticCarPark("HE12");
        StaticCarpark staticOnly = CreateStaticCarPark("HE13");
        LiveCarparkAvailability matchedLive = CreateLiveCarPark("he12");
        LiveCarparkAvailability liveOnly = CreateLiveCarPark("ZZ99");

        CarparkJoinSummary result = CarparkSearchLogic.JoinByCarParkNumber(
            [matchedStatic, staticOnly],
            [matchedLive, liveOnly]);

        JoinedCarpark match = Assert.Single(result.Matched);
        Assert.Equal("HE12", match.StaticCarpark.CarParkNo);
        Assert.Equal("he12", match.LiveAvailability?.CarParkNo);
        Assert.Equal("HE13", Assert.Single(result.StaticOnly).CarParkNo);
        Assert.Equal("ZZ99", Assert.Single(result.LiveOnly).CarParkNo);
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(499, true)]
    [InlineData(501, false)]
    public void GetNearbyCarParks_filters_by_radius_boundary(int distanceMetres, bool expectedIncluded)
    {
        const double originLatitude = 1.3000;
        const double originLongitude = 103.8000;
        StaticCarpark carPark = CreateStaticCarPark(
            "HE12",
            originLatitude + LatitudeOffset(distanceMetres),
            originLongitude);

        IReadOnlyList<NearbyCarparkResponse> result = CarparkSearchLogic.GetNearbyCarParks(
            [carPark],
            new Dictionary<string, LiveCarparkAvailability>(StringComparer.OrdinalIgnoreCase),
            originLatitude,
            originLongitude,
            500,
            DateTimeOffset.Parse("2026-08-10T21:29:37+08:00"));

        Assert.Equal(expectedIncluded, result.Count == 1);
    }

    [Theory]
    [InlineData("2026-08-10T21:24:37+08:00", false)]
    [InlineData("2026-08-10T21:24:38+08:00", false)]
    [InlineData("2026-08-10T21:24:36+08:00", true)]
    public void IsStale_uses_five_minute_boundary(string updateDatetime, bool expectedStale)
    {
        DateTimeOffset snapshotTime = DateTimeOffset.Parse("2026-08-10T21:29:37+08:00");

        bool isStale = CarparkSearchLogic.IsStale(snapshotTime, DateTimeOffset.Parse(updateDatetime));

        Assert.Equal(expectedStale, isStale);
    }

    private static double LatitudeOffset(double metres) => metres / 6_371_000d * (180d / Math.PI);

    private static StaticCarpark CreateStaticCarPark(string carParkNo, double latitude = 1.3, double longitude = 103.8) =>
        new(carParkNo, $"Address for {carParkNo}", latitude, longitude, "MULTI-STOREY CAR PARK", "ELECTRONIC PARKING", "WHOLE DAY", "NO", true, 5, 2.1, false);

    private static LiveCarparkAvailability CreateLiveCarPark(string carParkNo) =>
        new(carParkNo, DateTimeOffset.Parse("2026-08-10T21:28:38+08:00"), [new LotAvailability("C", 100, 30)]);
}

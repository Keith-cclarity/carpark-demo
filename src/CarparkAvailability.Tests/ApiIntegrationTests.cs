using System.Net;
using System.Net.Http.Json;
using CarparkAvailability.ApiApp.Models;
using CarparkAvailability.ApiApp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarparkAvailability.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task Nearby_returns_happy_path_payload()
    {
        await using ApiAppFactory factory = new(BuildStaticCarParks(), BuildSnapshot());
        using HttpClient client = factory.CreateClient();

        NearbyCarparksResponse? response = await client.GetFromJsonAsync<NearbyCarparksResponse>("/api/carparks/nearby?lat=1.3000&lng=103.8000&radius=500");

        Assert.NotNull(response);
        NearbyCarparkResponse carPark = Assert.Single(response.CarParks);
        Assert.Equal("HE12", carPark.CarParkNo);
        Assert.Equal(30, Assert.Single(carPark.Lots).LotsAvailable);
    }

    [Fact]
    public async Task Nearby_returns_empty_result_when_no_car_parks_match()
    {
        await using ApiAppFactory factory = new(BuildStaticCarParks(), BuildSnapshot());
        using HttpClient client = factory.CreateClient();

        NearbyCarparksResponse? response = await client.GetFromJsonAsync<NearbyCarparksResponse>("/api/carparks/nearby?lat=1.4100&lng=103.9300&radius=500");

        Assert.NotNull(response);
        Assert.Empty(response.CarParks);
    }

    [Fact]
    public async Task Nearby_returns_503_when_cache_is_empty()
    {
        await using ApiAppFactory factory = new(BuildStaticCarParks(), snapshot: null);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/carparks/nearby?lat=1.3000&lng=103.8000&radius=500");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static IReadOnlyDictionary<string, StaticCarpark> BuildStaticCarParks() => new Dictionary<string, StaticCarpark>(StringComparer.OrdinalIgnoreCase)
    {
        ["HE12"] = new StaticCarpark("HE12", "Blk 51 Circuit Road", 1.3002, 103.8000, "MULTI-STOREY CAR PARK", "ELECTRONIC PARKING", "WHOLE DAY", "NO", true, 5, 2.1, false)
    };

    private static LiveCarparkSnapshot BuildSnapshot() => new(
        DateTimeOffset.Parse("2026-08-10T21:29:37+08:00"),
        DateTimeOffset.UtcNow,
        new Dictionary<string, LiveCarparkAvailability>(StringComparer.OrdinalIgnoreCase)
        {
            ["HE12"] = new LiveCarparkAvailability("HE12", DateTimeOffset.Parse("2026-08-10T21:28:38+08:00"), [new LotAvailability("C", 105, 30)])
        });

    private sealed class ApiAppFactory(
        IReadOnlyDictionary<string, StaticCarpark> staticCarParks,
        LiveCarparkSnapshot? snapshot) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                ServiceDescriptor? poller = services.FirstOrDefault(descriptor => descriptor.ImplementationType == typeof(CarparkAvailabilityPoller));
                if (poller is not null)
                {
                    services.Remove(poller);
                }

                services.RemoveAll<IHdbCarparkRepository>();
                services.RemoveAll<ICarparkAvailabilitySnapshotStore>();
                services.AddSingleton<IHdbCarparkRepository>(new FakeHdbCarparkRepository(staticCarParks));
                services.AddSingleton<ICarparkAvailabilitySnapshotStore>(new FakeSnapshotStore(snapshot));
            });
        }
    }

    private sealed class FakeHdbCarparkRepository(IReadOnlyDictionary<string, StaticCarpark> carParks) : IHdbCarparkRepository
    {
        public IReadOnlyDictionary<string, StaticCarpark> CarParks { get; } = carParks;
    }

    private sealed class FakeSnapshotStore(LiveCarparkSnapshot? snapshot) : ICarparkAvailabilitySnapshotStore
    {
        private LiveCarparkSnapshot? currentSnapshot = snapshot;

        public LiveCarparkSnapshot? GetSnapshot() => currentSnapshot;

        public void Update(LiveCarparkSnapshot snapshot) => currentSnapshot = snapshot;
    }
}

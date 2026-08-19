using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace CarparkAvailability.WebApp.Services;

public sealed class CarparksApiClient(HttpClient httpClient)
{
    public async Task<NearbyCarparksResponse> GetNearbyAsync(double latitude, double longitude, double radiusMetres, CancellationToken cancellationToken = default)
    {
        string requestUri = string.Create(
            CultureInfo.InvariantCulture,
            $"/api/carparks/nearby?lat={latitude}&lng={longitude}&radius={radiusMetres}");

        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, cancellationToken);
        return await ReadResponseAsync<NearbyCarparksResponse>(response, cancellationToken);
    }

    public async Task<CarParkDetailResponse> GetCarParkAsync(string carParkNo, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync($"/api/carparks/{Uri.EscapeDataString(carParkNo)}", cancellationToken);
        return await ReadResponseAsync<CarParkDetailResponse>(response, cancellationToken);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new CarparksApiException("The API returned an empty response.");
        }

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        string message = problemDetails?.Detail
            ?? problemDetails?.Title
            ?? $"The API returned HTTP {(int)response.StatusCode}.";

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            throw new CarparksApiUnavailableException(message);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new CarparksApiNotFoundException(message);
        }

        throw new CarparksApiException(message);
    }
}

public class CarparksApiException(string message) : Exception(message);
public sealed class CarparksApiUnavailableException(string message) : CarparksApiException(message);
public sealed class CarparksApiNotFoundException(string message) : CarparksApiException(message);

public sealed record LotAvailabilityDto(string LotType, int TotalLots, int LotsAvailable);

public sealed record NearbyCarparkResponseDto(
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
    IReadOnlyList<LotAvailabilityDto> Lots)
{
    public int TotalLots => Lots.Sum(lot => lot.TotalLots);
    public int AvailableLots => Lots.Sum(lot => lot.LotsAvailable);
    public int OccupancyPercentage => TotalLots == 0 ? 0 : (int)Math.Round((1d - (AvailableLots / (double)TotalLots)) * 100d, MidpointRounding.AwayFromZero);
}

public sealed record NearbyCarparksResponse(
    DateTimeOffset SnapshotTime,
    long CacheAge,
    bool UsingLastKnownGood,
    IReadOnlyList<NearbyCarparkResponseDto> CarParks);

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
    IReadOnlyList<LotAvailabilityDto> Lots)
{
    public int TotalLots => Lots.Sum(lot => lot.TotalLots);
    public int AvailableLots => Lots.Sum(lot => lot.LotsAvailable);
    public int OccupancyPercentage => TotalLots == 0 ? 0 : (int)Math.Round((1d - (AvailableLots / (double)TotalLots)) * 100d, MidpointRounding.AwayFromZero);
}

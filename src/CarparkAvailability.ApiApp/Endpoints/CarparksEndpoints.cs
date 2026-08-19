using CarparkAvailability.ApiApp.Models;
using CarparkAvailability.ApiApp.Services;

namespace CarparkAvailability.ApiApp.Endpoints;

public static class CarparksEndpoints
{
    public static IEndpointRouteBuilder MapCarparksEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/carparks");

        group.MapGet("/nearby", GetNearbyCarParks)
            .WithName("GetNearbyCarParks");

        group.MapGet("/{carParkNo}", GetCarPark)
            .WithName("GetCarPark");

        return endpoints;
    }

    private static IResult GetNearbyCarParks(
        double lat,
        double lng,
        double? radius,
        ICarparkService carparkService)
    {
        radius ??= 500;

        Dictionary<string, string[]> validationErrors = new();
        if (lat is < -90 or > 90)
        {
            validationErrors[nameof(lat)] = ["Latitude must be between -90 and 90."];
        }

        if (lng is < -180 or > 180)
        {
            validationErrors[nameof(lng)] = ["Longitude must be between -180 and 180."];
        }

        if (radius <= 0)
        {
            validationErrors[nameof(radius)] = ["Radius must be greater than zero."];
        }

        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        try
        {
            NearbyCarparksResponse response = carparkService.GetNearby(lat, lng, radius.Value);
            return TypedResults.Ok(response);
        }
        catch (NoCarparkSnapshotException exception)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Live availability unavailable", detail: exception.Message);
        }
    }

    private static IResult GetCarPark(string carParkNo, ICarparkService carparkService)
    {
        try
        {
            CarParkDetailResponse? response = carparkService.GetCarPark(carParkNo);
            return response is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(response);
        }
        catch (NoCarparkSnapshotException exception)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Live availability unavailable", detail: exception.Message);
        }
    }
}

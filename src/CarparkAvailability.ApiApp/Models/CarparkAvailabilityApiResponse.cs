using System.Text.Json.Serialization;

namespace CarparkAvailability.ApiApp.Models;

public sealed class CarparkAvailabilityApiResponse
{
    [JsonPropertyName("items")]
    public List<CarparkAvailabilityItem>? Items { get; set; }
}

public sealed class CarparkAvailabilityItem
{
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("carpark_data")]
    public List<CarparkAvailabilityEntry>? CarparkData { get; set; }
}

public sealed class CarparkAvailabilityEntry
{
    [JsonPropertyName("carpark_number")]
    public string? CarparkNumber { get; set; }

    [JsonPropertyName("update_datetime")]
    public string? UpdateDatetime { get; set; }

    [JsonPropertyName("carpark_info")]
    public List<CarparkInfoEntry>? CarparkInfo { get; set; }
}

public sealed class CarparkInfoEntry
{
    [JsonPropertyName("lot_type")]
    public string? LotType { get; set; }

    [JsonPropertyName("total_lots")]
    public string? TotalLots { get; set; }

    [JsonPropertyName("lots_available")]
    public string? LotsAvailable { get; set; }
}

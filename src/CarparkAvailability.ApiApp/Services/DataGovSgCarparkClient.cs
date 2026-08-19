using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarparkAvailability.ApiApp.Models;
using Microsoft.Extensions.Options;

namespace CarparkAvailability.ApiApp.Services;

public interface IDataGovSgCarparkClient
{
    Task<LiveCarparkSnapshot> FetchLatestAsync(CancellationToken cancellationToken);
}

public sealed class DataGovSgCarparkClient(
    HttpClient httpClient,
    IOptions<DataGovSgOptions> options,
    TimeProvider timeProvider) : IDataGovSgCarparkClient
{
    private static readonly TimeSpan SingaporeOffset = TimeSpan.FromHours(8);

    public async Task<LiveCarparkSnapshot> FetchLatestAsync(CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "transport/carpark-availability");

        if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        }

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        CarparkAvailabilityApiResponse? payload = await response.Content.ReadFromJsonAsync<CarparkAvailabilityApiResponse>(cancellationToken);
        CarparkAvailabilityItem item = payload?.Items?.FirstOrDefault()
            ?? throw new InvalidOperationException("The data.gov.sg response did not contain any items.");

        if (!TryParseSnapshotTimestamp(item.Timestamp, out DateTimeOffset snapshotTime))
        {
            throw new InvalidOperationException("The data.gov.sg response contained an invalid snapshot timestamp.");
        }

        Dictionary<string, LiveCarparkAvailability> liveData = new(StringComparer.OrdinalIgnoreCase);
        foreach (CarparkAvailabilityEntry entry in item.CarparkData ?? [])
        {
            if (string.IsNullOrWhiteSpace(entry.CarparkNumber))
            {
                continue;
            }

            List<LotAvailability> lots = [];
            foreach (CarparkInfoEntry lot in entry.CarparkInfo ?? [])
            {
                if (string.IsNullOrWhiteSpace(lot.LotType)
                    || !int.TryParse(lot.TotalLots, NumberStyles.Integer, CultureInfo.InvariantCulture, out int totalLots)
                    || !int.TryParse(lot.LotsAvailable, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lotsAvailable))
                {
                    continue;
                }

                lots.Add(new LotAvailability(lot.LotType.Trim().ToUpperInvariant(), totalLots, lotsAvailable));
            }

            liveData[entry.CarparkNumber.Trim()] = new LiveCarparkAvailability(
                entry.CarparkNumber.Trim(),
                ParseSingaporeDateTime(entry.UpdateDatetime),
                lots.OrderBy(lot => lot.LotType, StringComparer.OrdinalIgnoreCase).ToArray());
        }

        return new LiveCarparkSnapshot(snapshotTime, timeProvider.GetUtcNow(), liveData);
    }

    private static bool TryParseSnapshotTimestamp(string? value, out DateTimeOffset snapshotTime) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out snapshotTime);

    private static DateTimeOffset? ParseSingaporeDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTimeOffset offsetDateTime))
        {
            return offsetDateTime;
        }

        if (DateTime.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime localDateTime))
        {
            return new DateTimeOffset(localDateTime, SingaporeOffset);
        }

        return null;
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using CarparkAvailability.WebApp.Services;

namespace CarparkAvailability.WebApp.Components.Pages;

public partial class Home : ComponentBase, IAsyncDisposable
{
    [Inject] private CarparksApiClient ApiClient { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private readonly List<NearbyCarparkResponseDto> allCarParks = [];
    private readonly List<NearbyCarparkResponseDto> filteredCarParks = [];
    private readonly List<string> availableCarParkTypes = [];
    private DotNetObjectReference<Home>? dotNetReference;
    private ElementReference mapElement;
    private ElementReference searchInput;
    private NearbyLoadState loadState = NearbyLoadState.Idle;
    private CarParkDetailResponse? selectedDetail;
    private string? selectedCarParkNo;
    private string searchText = string.Empty;
    private string statusMessage = string.Empty;
    private string selectedLotType = string.Empty;
    private string selectedCarParkType = string.Empty;
    private bool showAvailableOnly;
    private bool nightParkingOnly;
    private bool hasLoadedResponse;
    private bool showStaleBanner;
    private bool showLastKnownGoodBanner;
    private bool isMapReady;
    private bool hasPendingMapRefresh;
    private DateTimeOffset? lastSnapshotTime;
    private long lastCacheAgeSeconds;
    private DestinationSelection? selectedDestination;
    private bool centerMapOnDestination;

    private bool HasGoogleMapsApiKey =>
        !string.IsNullOrWhiteSpace(Configuration["GoogleMaps:ApiKey"])
        && !Configuration["GoogleMaps:ApiKey"]!.Contains("{{", StringComparison.Ordinal);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && HasGoogleMapsApiKey)
        {
            dotNetReference = DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("smartParkingMap.initialize", mapElement, searchInput, dotNetReference);
            isMapReady = true;
        }

        if (isMapReady && hasPendingMapRefresh)
        {
            hasPendingMapRefresh = false;
            await JsRuntime.InvokeVoidAsync("smartParkingMap.updateMarkers", filteredCarParks.Select(MapMarkerData.FromCarPark).ToArray(), selectedCarParkNo);

            if (centerMapOnDestination && selectedDestination is not null)
            {
                centerMapOnDestination = false;
                await JsRuntime.InvokeVoidAsync("smartParkingMap.centerOnPlace", selectedDestination.Latitude, selectedDestination.Longitude);
            }
        }
    }

    [JSInvokable]
    public async Task OnPlaceSelected(string label, double latitude, double longitude)
    {
        searchText = label;
        selectedDestination = new DestinationSelection(label, latitude, longitude);
        await LoadNearbyCarParksAsync();
    }

    [JSInvokable]
    public async Task OnMarkerSelected(string carParkNo)
    {
        await OnCarParkSelectedAsync(carParkNo);
    }

    private async Task LoadNearbyCarParksAsync()
    {
        if (selectedDestination is null)
        {
            return;
        }

        loadState = NearbyLoadState.Loading;
        statusMessage = string.Empty;
        selectedDetail = null;
        selectedCarParkNo = null;
        StateHasChanged();

        try
        {
            NearbyCarparksResponse response = await ApiClient.GetNearbyAsync(selectedDestination.Latitude, selectedDestination.Longitude, 500);
            allCarParks.Clear();
            allCarParks.AddRange(response.CarParks);
            lastSnapshotTime = response.SnapshotTime;
            lastCacheAgeSeconds = response.CacheAge;
            hasLoadedResponse = true;
            showLastKnownGoodBanner = response.UsingLastKnownGood;
            showStaleBanner = response.CarParks.Any(carPark => carPark.IsStale);
            RefreshAvailableCarParkTypes();
            centerMapOnDestination = true;
            ApplyFilters();
            loadState = allCarParks.Count == 0 ? NearbyLoadState.Empty : NearbyLoadState.Loaded;

            if (filteredCarParks.Count > 0)
            {
                await OnCarParkSelectedAsync(filteredCarParks[0].CarParkNo, moveMap: false);
            }
            else
            {
                hasPendingMapRefresh = true;
            }
        }
        catch (CarparksApiUnavailableException exception)
        {
            SetFailureState(NearbyLoadState.Unavailable, exception.Message);
        }
        catch (CarparksApiException exception)
        {
            SetFailureState(NearbyLoadState.Error, exception.Message);
        }
    }

    private void SetFailureState(NearbyLoadState state, string message)
    {
        allCarParks.Clear();
        filteredCarParks.Clear();
        availableCarParkTypes.Clear();
        selectedDetail = null;
        selectedCarParkNo = null;
        showLastKnownGoodBanner = false;
        showStaleBanner = false;
        statusMessage = message;
        loadState = state;
        hasPendingMapRefresh = true;
        hasLoadedResponse = false;
        centerMapOnDestination = false;
    }

    private async Task OnCarParkSelectedAsync(string carParkNo, bool moveMap = true)
    {
        selectedCarParkNo = carParkNo;
        selectedDetail = await ApiClient.GetCarParkAsync(carParkNo);
        hasPendingMapRefresh = true;

        if (moveMap && isMapReady && filteredCarParks.FirstOrDefault(carPark => carPark.CarParkNo == carParkNo) is { } selectedCarPark)
        {
            await JsRuntime.InvokeVoidAsync("smartParkingMap.focusMarker", carParkNo, selectedCarPark.Latitude, selectedCarPark.Longitude);
        }
    }

    private async Task OnFiltersChangedAsync()
    {
        string? previousSelection = selectedCarParkNo;
        ApplyFilters();

        if (selectedCarParkNo is not null && (selectedDetail is null || !string.Equals(previousSelection, selectedCarParkNo, StringComparison.OrdinalIgnoreCase)))
        {
            await OnCarParkSelectedAsync(selectedCarParkNo, moveMap: false);
        }
    }

    private void ApplyFilters()
    {
        filteredCarParks.Clear();
        IEnumerable<NearbyCarparkResponseDto> query = allCarParks;

        if (showAvailableOnly)
        {
            query = query.Where(carPark => carPark.AvailableLots > 0);
        }

        if (!string.IsNullOrWhiteSpace(selectedLotType))
        {
            query = query.Where(carPark => MatchesLotTypeFilter(carPark, selectedLotType));
        }

        if (nightParkingOnly)
        {
            query = query.Where(carPark => carPark.NightParking);
        }

        if (!string.IsNullOrWhiteSpace(selectedCarParkType))
        {
            query = query.Where(carPark => string.Equals(carPark.CarParkType, selectedCarParkType, StringComparison.OrdinalIgnoreCase));
        }

        filteredCarParks.AddRange(query.OrderBy(carPark => carPark.DistanceMetres));

        if (selectedCarParkNo is null || filteredCarParks.All(carPark => carPark.CarParkNo != selectedCarParkNo))
        {
            selectedCarParkNo = filteredCarParks.FirstOrDefault()?.CarParkNo;
            selectedDetail = filteredCarParks.Count == 0 ? null : null;
        }

        if (filteredCarParks.Count == 0)
        {
            selectedCarParkNo = null;
            selectedDetail = null;
        }

        hasPendingMapRefresh = true;
        StateHasChanged();
    }

    private void RefreshAvailableCarParkTypes()
    {
        availableCarParkTypes.Clear();
        availableCarParkTypes.AddRange(allCarParks
            .Select(carPark => carPark.CarParkType)
            .Where(carParkType => !string.IsNullOrWhiteSpace(carParkType))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(carParkType => carParkType, StringComparer.OrdinalIgnoreCase));
    }

    private static bool MatchesLotTypeFilter(NearbyCarparkResponseDto carPark, string selectedLotType)
    {
        if (selectedLotType == "M")
        {
            return carPark.Lots.Any(lot => lot.LotType is "S" or "Y");
        }

        return carPark.Lots.Any(lot => string.Equals(lot.LotType, selectedLotType, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatDistance(int distanceMetres) => distanceMetres >= 1000
        ? $"{distanceMetres / 1000d:F1} km"
        : $"{distanceMetres} m";

    private static string FormatLotTypes(IEnumerable<LotAvailabilityDto> lots)
    {
        string[] labels = lots
            .Select(lot => GetLotTypeLabel(lot.LotType))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return labels.Length == 0 ? "No live data" : string.Join(", ", labels);
    }

    private static string GetFreshnessLabel(DateTimeOffset? updateDatetime, bool isStale)
    {
        if (updateDatetime is null)
        {
            return "Live data unavailable";
        }

        return isStale
            ? $"Stale · {updateDatetime:HH:mm}"
            : $"Updated · {updateDatetime:HH:mm}";
    }

    private static string FormatBoolean(bool value) => value ? "Yes" : "No";
    private static string FormatHeight(double? height) => height.HasValue ? $"{height.Value:0.##} m" : "—";

    private static string GetLotTypeLabel(string lotType) => lotType switch
    {
        "C" => "Cars",
        "H" => "Heavy vehicles",
        "S" or "Y" => "Motorcycles",
        _ => lotType
    };

    public async ValueTask DisposeAsync()
    {
        dotNetReference?.Dispose();

        if (isMapReady)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("smartParkingMap.dispose");
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private enum NearbyLoadState
    {
        Idle,
        Loading,
        Loaded,
        Empty,
        Unavailable,
        Error
    }

    private sealed record DestinationSelection(string Label, double Latitude, double Longitude);
    private sealed record MapMarkerData(string CarParkNo, string Address, double Latitude, double Longitude, int AvailableLots, int TotalLots, bool IsStale)
    {
        public static MapMarkerData FromCarPark(NearbyCarparkResponseDto carPark) =>
            new(carPark.CarParkNo, carPark.Address, carPark.Latitude, carPark.Longitude, carPark.AvailableLots, carPark.TotalLots, carPark.IsStale);
    }
}

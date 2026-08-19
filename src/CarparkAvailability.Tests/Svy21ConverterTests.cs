using CarparkAvailability.ApiApp.Services;

namespace CarparkAvailability.Tests;

public sealed class Svy21ConverterTests
{
    private readonly Svy21Converter converter = new();

    [Theory]
    [InlineData(28001.642, 38744.572, 1.366666, 103.833333)]
    [InlineData(30629.967, 39105.269, 1.3699278977737488, 103.856950349764668)]
    public void ConvertToWgs84_converts_known_reference_points(double easting, double northing, double expectedLatitude, double expectedLongitude)
    {
        (double latitude, double longitude) = converter.ConvertToWgs84(easting, northing);

        Assert.Equal(expectedLatitude, latitude, 6);
        Assert.Equal(expectedLongitude, longitude, 6);
    }
}

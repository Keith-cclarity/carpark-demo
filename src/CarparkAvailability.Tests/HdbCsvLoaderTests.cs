using System.Text;
using CarparkAvailability.ApiApp.Models;
using CarparkAvailability.ApiApp.Services;

namespace CarparkAvailability.Tests;

public sealed class HdbCsvLoaderTests
{
    private readonly HdbCsvLoader loader = new(new StubSvy21Converter());

    [Fact]
    public void Load_parses_a_normal_row()
    {
        IReadOnlyDictionary<string, StaticCarpark> result = loader.Load(new StringReader(BuildCsv(
            "ACB,Albert Centre,30000.1,31000.2,BASEMENT CAR PARK,ELECTRONIC PARKING,WHOLE DAY,NO,YES,2,2.1,Y")));

        StaticCarpark carPark = Assert.Single(result.Values);
        Assert.Equal("ACB", carPark.CarParkNo);
        Assert.Equal("Albert Centre", carPark.Address);
        Assert.Equal(1.3, carPark.Latitude, 3);
        Assert.Equal(103.8, carPark.Longitude, 3);
        Assert.True(carPark.NightParking);
        Assert.Equal(2, carPark.CarParkDecks);
        Assert.NotNull(carPark.GantryHeight);
        Assert.Equal(2.1, carPark.GantryHeight.Value, 3);
        Assert.True(carPark.CarParkBasement);
    }

    [Fact]
    public void Load_skips_rows_with_missing_car_park_number()
    {
        IReadOnlyDictionary<string, StaticCarpark> result = loader.Load(new StringReader(BuildCsv(
            ",Albert Centre,30000.1,31000.2,BASEMENT CAR PARK,ELECTRONIC PARKING,WHOLE DAY,NO,YES,2,2.1,Y")));

        Assert.Empty(result);
    }

    [Fact]
    public void Load_handles_non_numeric_values_gracefully()
    {
        IReadOnlyDictionary<string, StaticCarpark> result = loader.Load(new StringReader(BuildCsv(
            "ACB,Albert Centre,30000.1,31000.2,BASEMENT CAR PARK,ELECTRONIC PARKING,WHOLE DAY,NO,YES,unknown,nope,Y")));

        StaticCarpark carPark = Assert.Single(result.Values);
        Assert.Null(carPark.CarParkDecks);
        Assert.Null(carPark.GantryHeight);
    }

    private static string BuildCsv(string row) => string.Join('\n',
        "car_park_no,address,x_coord,y_coord,car_park_type,type_of_parking_system,short_term_parking,free_parking,night_parking,car_park_decks,gantry_height,car_park_basement",
        row);

    private sealed class StubSvy21Converter : ISvy21Converter
    {
        public (double Latitude, double Longitude) ConvertToWgs84(double easting, double northing) => (1.3, 103.8);
    }
}

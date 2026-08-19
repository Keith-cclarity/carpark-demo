using System.Globalization;
using CarparkAvailability.ApiApp.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace CarparkAvailability.ApiApp.Services;

public interface IHdbCsvLoader
{
    IReadOnlyDictionary<string, StaticCarpark> Load(string csvPath);
    IReadOnlyDictionary<string, StaticCarpark> Load(TextReader reader);
}

public sealed class HdbCsvLoader(ISvy21Converter svy21Converter) : IHdbCsvLoader
{
    public IReadOnlyDictionary<string, StaticCarpark> Load(string csvPath)
    {
        using StreamReader reader = File.OpenText(csvPath);
        return Load(reader);
    }

    public IReadOnlyDictionary<string, StaticCarpark> Load(TextReader reader)
    {
        CsvConfiguration configuration = new(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using CsvReader csv = new(reader, configuration);
        csv.Context.RegisterClassMap<HdbCarparkCsvRowMap>();

        Dictionary<string, StaticCarpark> carparks = new(StringComparer.OrdinalIgnoreCase);

        foreach (HdbCarparkCsvRow row in csv.GetRecords<HdbCarparkCsvRow>())
        {
            if (string.IsNullOrWhiteSpace(row.CarParkNo)
                || !TryParseDouble(row.XCoord, out double easting)
                || !TryParseDouble(row.YCoord, out double northing))
            {
                continue;
            }

            (double latitude, double longitude) = svy21Converter.ConvertToWgs84(easting, northing);
            carparks[row.CarParkNo.Trim()] = new StaticCarpark(
                row.CarParkNo.Trim(),
                row.Address?.Trim() ?? string.Empty,
                latitude,
                longitude,
                row.CarParkType?.Trim() ?? string.Empty,
                row.TypeOfParkingSystem?.Trim() ?? string.Empty,
                row.ShortTermParking?.Trim() ?? string.Empty,
                row.FreeParking?.Trim() ?? string.Empty,
                ParseBoolean(row.NightParking, "YES"),
                TryParseInt(row.CarParkDecks, out int carParkDecks) ? carParkDecks : null,
                TryParseDouble(row.GantryHeight, out double gantryHeight) ? gantryHeight : null,
                ParseBoolean(row.CarParkBasement, "Y"));
        }

        return carparks;
    }

    private static bool TryParseDouble(string? value, out double parsed) =>
        double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsed);

    private static bool TryParseInt(string? value, out int parsed) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);

    private static bool ParseBoolean(string? value, string trueValue) =>
        string.Equals(value?.Trim(), trueValue, StringComparison.OrdinalIgnoreCase);
}

public sealed class HdbCarparkCsvRowMap : ClassMap<HdbCarparkCsvRow>
{
    public HdbCarparkCsvRowMap()
    {
        Map(row => row.CarParkNo).Name("car_park_no");
        Map(row => row.Address).Name("address");
        Map(row => row.XCoord).Name("x_coord");
        Map(row => row.YCoord).Name("y_coord");
        Map(row => row.CarParkType).Name("car_park_type");
        Map(row => row.TypeOfParkingSystem).Name("type_of_parking_system");
        Map(row => row.ShortTermParking).Name("short_term_parking");
        Map(row => row.FreeParking).Name("free_parking");
        Map(row => row.NightParking).Name("night_parking");
        Map(row => row.CarParkDecks).Name("car_park_decks");
        Map(row => row.GantryHeight).Name("gantry_height");
        Map(row => row.CarParkBasement).Name("car_park_basement");
    }
}

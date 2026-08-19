using CarparkAvailability.ApiApp.Models;
using Microsoft.Extensions.Options;

namespace CarparkAvailability.ApiApp.Services;

public interface IHdbCarparkRepository
{
    IReadOnlyDictionary<string, StaticCarpark> CarParks { get; }
}

public sealed class HdbCarparkRepository : IHdbCarparkRepository
{
    public HdbCarparkRepository(IOptions<StaticDataOptions> options, IHdbCsvLoader csvLoader, IWebHostEnvironment environment)
    {
        string csvPath = options.Value.CsvPath;
        if (!Path.IsPathRooted(csvPath))
        {
            csvPath = Path.Combine(environment.ContentRootPath, csvPath);
        }

        CarParks = csvLoader.Load(csvPath);
    }

    public IReadOnlyDictionary<string, StaticCarpark> CarParks { get; }
}

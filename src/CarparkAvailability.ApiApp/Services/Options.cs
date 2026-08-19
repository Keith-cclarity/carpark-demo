namespace CarparkAvailability.ApiApp.Services;

public sealed class DataGovSgOptions
{
    public string BaseUrl { get; set; } = "https://api.data.gov.sg/v1/";
    public string? ApiKey { get; set; }
}

public sealed class StaticDataOptions
{
    public string CsvPath { get; set; } = "Data/HDBCarparkInformation.csv";
}

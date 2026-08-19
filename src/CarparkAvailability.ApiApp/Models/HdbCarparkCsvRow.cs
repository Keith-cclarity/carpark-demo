namespace CarparkAvailability.ApiApp.Models;

public sealed class HdbCarparkCsvRow
{
    public string? CarParkNo { get; set; }
    public string? Address { get; set; }
    public string? XCoord { get; set; }
    public string? YCoord { get; set; }
    public string? CarParkType { get; set; }
    public string? TypeOfParkingSystem { get; set; }
    public string? ShortTermParking { get; set; }
    public string? FreeParking { get; set; }
    public string? NightParking { get; set; }
    public string? CarParkDecks { get; set; }
    public string? GantryHeight { get; set; }
    public string? CarParkBasement { get; set; }
}

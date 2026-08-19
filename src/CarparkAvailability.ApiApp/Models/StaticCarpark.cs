namespace CarparkAvailability.ApiApp.Models;

public sealed record StaticCarpark(
    string CarParkNo,
    string Address,
    double Latitude,
    double Longitude,
    string CarParkType,
    string ParkingSystem,
    string ShortTermParking,
    string FreeParking,
    bool NightParking,
    int? CarParkDecks,
    double? GantryHeight,
    bool CarParkBasement,
    bool StaticDataAvailable = true);

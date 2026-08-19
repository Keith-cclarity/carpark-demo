using CarparkAvailability.ApiApp.Endpoints;
using CarparkAvailability.ApiApp.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<DataGovSgOptions>(builder.Configuration.GetSection("DataGovSg"));
builder.Services.Configure<StaticDataOptions>(builder.Configuration.GetSection("StaticData"));
builder.Services.AddSingleton<ISvy21Converter, Svy21Converter>();
builder.Services.AddSingleton<IHdbCsvLoader, HdbCsvLoader>();
builder.Services.AddSingleton<IHdbCarparkRepository, HdbCarparkRepository>();
builder.Services.AddSingleton<ICarparkAvailabilitySnapshotStore, CarparkAvailabilitySnapshotStore>();
builder.Services.AddSingleton<ICarparkService, CarparkService>();
builder.Services.AddHttpClient<IDataGovSgCarparkClient, DataGovSgCarparkClient>((serviceProvider, client) =>
{
    DataGovSgOptions options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DataGovSgOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
});
builder.Services.AddHostedService<CarparkAvailabilityPoller>();

WebApplication app = builder.Build();

app.Services.GetRequiredService<IHdbCarparkRepository>();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api", () => Results.Ok(new
{
    name = "Smart Parking Navigator API",
    status = "Ready",
    endpoints = new[]
    {
        "/api/carparks/nearby",
        "/api/carparks/{carParkNo}"
    }
}));

app.MapCarparksEndpoints();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;

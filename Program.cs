using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Cors;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

//string googleMapsApiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
//string googlePlacesApiKey = Environment.GetEnvironmentVariable("GOOGLE_PLACES_API_KEY");
//string googleDirectionsApiKey = Environment.GetEnvironmentVariable("GOOGLE_DIRECTIONS_API_KEY");
string hereApiKey = Environment.GetEnvironmentVariable("HERE_API_KEY");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TrafficService>(sp =>
    new TrafficService(sp.GetRequiredService<HttpClient>(), hereApiKey)
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapGet("/api/traffic/{city}", async (string city, TrafficService trafficService) =>
{
    // Step 1: Get city center and approximate bounds
    var cityCenter = await trafficService.GetCenterForCityAsync(city);
    double latMin = cityCenter.Latitude - 0.05;
    double latMax = cityCenter.Latitude + 0.05;
    double lngMin = cityCenter.Longitude - 0.05;
    double lngMax = cityCenter.Longitude + 0.05;
    double gridSize = 0.01;

    var gridCells = await trafficService.GenerateGrid(latMin, latMax, lngMin, lngMax, gridSize);

    var trafficData = await trafficService.GetTrafficForCityGridAsync(gridCells);

    if (trafficData == null)
    {
        return Results.NotFound(new { message = "No traffic data found for the specified city." });
    }

    return Results.Ok(trafficData);
}).WithName("GetTrafficData").WithOpenApi();

app.MapGet("/api/city-centre/{city}", async (string city, TrafficService trafficService) =>
{
    var centreData = await trafficService.GetCenterForCityAsync(city);

    if (centreData == null)
    {
        return Results.NotFound(new { message = "Failed to fetch centre for the specified city." });
    }

    return Results.Ok(centreData);
}).WithName("GetCityCentreData").WithOpenApi();


app.Run();


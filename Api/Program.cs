using Api.Contracts;
using YetAnotherGarminConnectClient;
using YetAnotherGarminConnectClient.Dto;
using Microsoft.Extensions.Caching.Memory;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddMemoryCache();
var app = builder.Build();

app.UseCors(builder => builder
.AllowAnyOrigin()
.AllowAnyHeader()
.AllowAnyMethod());

app.MapGet("/", () => new
{
    name = "yet-another-garmin-connect-client-api",
    projectUrl = "https://github.com/lswiderski/yet-another-garmin-connect-client",
    requestEndpoint = "/upload"
});

app.MapPost("/upload", async (BodyCompositionRequest request, IMemoryCache memoryCache) =>
{
    try
    {
        memoryCache.TryGetValue<IClient>(request.ClientID ?? "", out var garminClient);
        if(garminClient is null)
        {
            garminClient = await ClientFactory.Create();
        }
        var userProfileSettings = new UserProfileSettings
        {
            Age = 40,
            Height = 180,
        };
        var garminWeightScaleDTO = new GarminWeightScaleDTO
        {
            TimeStamp = DateTime.UtcNow,
            Weight = request.Weight,
            PercentFat = request.PercentFat,
            PercentHydration = request.PercentHydration,
            BoneMass = request.BoneMass,
            MuscleMass = request.MuscleMass,
            VisceralFatRating = request.VisceralFatRating.HasValue ? (byte)Math.Round(request.VisceralFatRating.Value) : null,
            VisceralFatMass = request.VisceralFatMass,
            PhysiqueRating = request.PhysiqueRating.HasValue ? (byte)Math.Round(request.PhysiqueRating.Value) : null,
            MetabolicAge = request.MetabolicAge.HasValue ? (byte)Math.Round(request.MetabolicAge.Value) : null,
            BodyMassIndex = request.bodyMassIndex,
            Email = request.Email,
            Password = request.Password,
            
        };

        var uploadResult = await garminClient.UploadWeight(garminWeightScaleDTO, userProfileSettings, request.MFACode);

        if (uploadResult.IsSuccess)
        {
            return Results.Created("/", new { uploadResult.AuthStatus });
        }
        if (uploadResult.MFACodeRequested)
        {
            var clientId = Guid.NewGuid().ToString();
            memoryCache.Set(clientId, garminClient, TimeSpan.FromMinutes(10));

            return Results.Ok(new { clientId, uploadResult.AuthStatus });
        }
        return Results.BadRequest($"AuthStatus: {uploadResult.AuthStatus}, Last Error log: {uploadResult.ErrorLogs.LastOrDefault()}");

    }
    catch (GarminClientException ex)
    {
        return Results.BadRequest($"AuthStatus: {ex.AuthStatus}, message: {ex.Message}");
    }
    catch (Exception ex)
    {
        var logs = Logger.GetLogs();
        var errorLogs = Logger.GetErrorLogs();
        return Results.BadRequest($"Last Error log: {errorLogs.LastOrDefault()}, message: {ex.Message}");
    }
});


app.Run();

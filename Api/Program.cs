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
    uploadBodyCompositiontEndpoint = "/upload",
    uploadBloodPressureEndpoint = "/uploadbloodpressure"
});

app.MapPost("/upload", async (BodyCompositionRequest request, IMemoryCache memoryCache) =>
{
    try
    {
        var userProfileSettings = new UserProfileSettings
        {
            Age = 40,
            Height = 180,
        };

        var garminWeightScaleDTO = new GarminWeightScaleDTO
        {
            TimeStamp = request.TimeStamp == null || request.TimeStamp == -1 ? DateTime.UtcNow : DateTime.UnixEpoch.AddSeconds(request.TimeStamp.Value),
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
        };

        var credentials = new CredentialsData
        {
            Email = request.Email,
            Password = request.Password,
            AccessToken = request.AccessToken,
            TokenSecret = request.TokenSecret,
        };

        if (request.CreateOnlyFile)
        {
            var fitFile = FitFileCreator.CreateWeightBodyCompositionFitFile(garminWeightScaleDTO, userProfileSettings);
            if(fitFile == null) 
            {
                Results.BadRequest("Fit file is empty");
            }
            return Results.File(fitFile, fileDownloadName: $"Activity_{garminWeightScaleDTO.TimeStamp.ToShortDateString()}.fit");
        }
        else
        {
            memoryCache.TryGetValue<IClient>(request.ClientID ?? "", out var garminClient);
            if (garminClient is null)
            {
                garminClient = await ClientFactory.Create();
            }

            var uploadResult = await garminClient.UploadWeight(garminWeightScaleDTO, userProfileSettings, credentials, request.MFACode);

            if (uploadResult.IsSuccess)
            {
                return Results.Created("/", uploadResult);
            }
            if (uploadResult.MFACodeRequested)
            {
                var clientId = Guid.NewGuid().ToString();
                memoryCache.Set(clientId, garminClient, TimeSpan.FromMinutes(10));

                return Results.Ok(new { clientId, uploadResult });
            }
            return Results.BadRequest($"AuthStatus: {uploadResult.AuthStatus}, Last Error log: {uploadResult.ErrorLogs.LastOrDefault()}");
        }
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

app.MapPost("/uploadbloodpressure", async (BloodPressureRequest request, IMemoryCache memoryCache) =>
{
    try
    {
        memoryCache.TryGetValue<IClient>(request.ClientID ?? "", out var garminClient);
        if (garminClient is null)
        {
            garminClient = await ClientFactory.Create();
        }

        var bloodDto = new BloodPressureDataDTO
        {
            TimeStamp = request.TimeStamp == null || request.TimeStamp == -1 ? DateTime.UtcNow : DateTime.UnixEpoch.AddSeconds(request.TimeStamp.Value),
            HeartRate = request.HeartRate,
            SystolicPressure = request.SystolicPressure,
            DiastolicPressure = request.DiastolicPressure,
            Email = request.Email,
            Password = request.Password,
        };

        var uploadResult = await garminClient.UploadBlood(bloodDto, request.MFACode);

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

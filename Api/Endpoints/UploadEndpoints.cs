using Api.Contracts;
using Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using YetAnotherGarminConnectClient;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace Api.Endpoints;

public static class UploadEndpoints
{
    public static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/upload", async (
            BodyCompositionRequest request,
            IMemoryCache memoryCache,
            IOptions<AppSettings> appSettingsOpt,
            ILogger<Program> logger) =>
        {
            var appSettings = appSettingsOpt.Value;

            logger.LogInformation("Received /upload request for email: {Email}", request.Email);

            try
            {
                var userProfileSettings = new UserProfileSettings
                {
                    Age = appSettings.UserProfileSettings.Age,
                    Height = appSettings.UserProfileSettings.Height,
                };

                if (request.UserProfile != null)
                {
                    userProfileSettings = new UserProfileSettings()
                    {
                        Age = request.UserProfile.Age,
                        Height = request.UserProfile.Height
                    };
                }

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

                logger.LogDebug("Prepared GarminWeightScaleDTO: {@GarminWeightScaleDTO}", garminWeightScaleDTO);

                var credentials = new CredentialsData
                {
                    Email = request.Email,
                    Password = request.Password,
                    AccessToken = request.AccessToken,
                    TokenSecret = request.TokenSecret,
                };

                if (request.CreateOnlyFile)
                {
                    logger.LogInformation("CreateOnlyFile requested for email: {Email}", request.Email);
                    var fitFile = FitFileCreator.CreateWeightBodyCompositionFitFile(garminWeightScaleDTO, userProfileSettings);
                    if (fitFile == null)
                    {
                        logger.LogWarning("Fit file is empty for email: {Email}", request.Email);
                        return Results.BadRequest("Fit file is empty");
                    }
                    logger.LogInformation("Fit file created successfully for email: {Email}", request.Email);
                    return Results.File(fitFile, fileDownloadName: $"Activity_{garminWeightScaleDTO.TimeStamp.ToShortDateString()}.fit");
                }
                else
                {
                    memoryCache.TryGetValue<IClient>(request.ClientID ?? "", out var garminClient);
                    if (garminClient is null)
                    {
                        logger.LogInformation("No cached client found. Creating new Garmin client.");
                        garminClient = await ClientFactory.Create();
                    }

                    var uploadResult = await garminClient.UploadWeight(garminWeightScaleDTO, userProfileSettings, credentials, request.MFACode);

                    if (uploadResult.IsSuccess)
                    {
                        logger.LogInformation("Weight upload successful for email: {Email}", request.Email);
                        return Results.Created("/", new { uploadResult });
                    }
                    if (uploadResult.MFACodeRequested)
                    {
                        var clientId = Guid.NewGuid().ToString();
                        memoryCache.Set(clientId, garminClient, TimeSpan.FromMinutes(10));
                        logger.LogWarning("MFA code requested for email: {Email}. ClientId: {ClientId}", request.Email, clientId);

                        return Results.Ok(new { clientId, uploadResult });
                    }
                    logger.LogError("Weight upload failed. AuthStatus: {AuthStatus}, LastError: {LastError}",
                        uploadResult.AuthStatus, uploadResult.ErrorLogs.LastOrDefault());
                    return Results.BadRequest($"AuthStatus: {uploadResult.AuthStatus}, Last Error log: {uploadResult.ErrorLogs.LastOrDefault()}");
                }
            }
            catch (GarminClientException ex)
            {
                logger.LogError(ex, "GarminClientException occurred. AuthStatus: {AuthStatus}", ex.AuthStatus);
                return Results.BadRequest($"AuthStatus: {ex.AuthStatus}, message: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception occurred during /upload.");
                return Results.BadRequest($"Exception: {ex.Message}");
            }
        });
    }
}
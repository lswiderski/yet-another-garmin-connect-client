using Api.Contracts;
using Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using YetAnotherGarminConnectClient;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace Api.Endpoints;

public static class BloodPressureEndpoints
{
    public static void MapBloodPressureEndpoints(this WebApplication app)
    {
        app.MapPost("/uploadbloodpressure", async (
            BloodPressureRequest request,
            IMemoryCache memoryCache,
            IOptions<AppSettings> appSettingsOpt,
            ILogger<Program> logger) =>
        {
            var appSettings = appSettingsOpt.Value;

            logger.LogInformation("Received /uploadbloodpressure request for email: {Email}", request.Email);

            try
            {
                memoryCache.TryGetValue<IClient>(request.ClientID ?? "", out var garminClient);
                if (garminClient is null)
                {
                    logger.LogInformation("No cached client found. Creating new Garmin client.");
                    garminClient = await ClientFactory.Create(GarminServerHelper.GetServer(request.Server));
                }

                if (appSettings.Auth != null)
                {
                    if (!string.IsNullOrEmpty(appSettings.Auth.Email))
                    {
                        request.Email = appSettings.Auth.Email;
                        logger.LogInformation("Using email from app settings.");
                    }
                    if (!string.IsNullOrEmpty(appSettings.Auth.Password))
                    {
                        request.Password = appSettings.Auth.Password;
                        logger.LogInformation("Using password from app settings.");
                    }

                    if (!string.IsNullOrEmpty(appSettings.Auth.Server))
                    {
                        request.Server = appSettings.Auth.Server;
                        logger.LogInformation("Using server from app settings.");
                    }
                }

                var bloodDto = new BloodPressureDataDTO
                {
                    TimeStamp = request.TimeStamp == null || request.TimeStamp == -1
                        ? DateTime.UtcNow
                        : DateTime.UnixEpoch.AddSeconds(request.TimeStamp.Value),
                    HeartRate = request.HeartRate,
                    SystolicPressure = request.SystolicPressure,
                    DiastolicPressure = request.DiastolicPressure,
                    Email = request.Email,
                    Password = request.Password,
                };

               

                logger.LogDebug("Prepared BloodPressureDataDTO: {@BloodDto}", bloodDto);

                var uploadResult = await garminClient.UploadBlood(bloodDto, request.MFACode);

                if (uploadResult.IsSuccess)
                {
                    logger.LogInformation("Blood pressure upload successful for email: {Email}", request.Email);
                    return Results.Created("/", new { uploadResult.AuthStatus });
                }
                if (uploadResult.MFACodeRequested)
                {
                    var clientId = Guid.NewGuid().ToString();
                    memoryCache.Set(clientId, garminClient, TimeSpan.FromMinutes(10));
                    logger.LogWarning("MFA code requested for email: {Email}. ClientId: {ClientId}", request.Email, clientId);

                    return Results.Ok(new { clientId, uploadResult.AuthStatus });
                }

                logger.LogError("Blood pressure upload failed. AuthStatus: {AuthStatus}, LastError: {LastError}",
                    uploadResult.AuthStatus, uploadResult.ErrorLogs.LastOrDefault());
                return Results.BadRequest($"AuthStatus: {uploadResult.AuthStatus}, Last Error log: {uploadResult.ErrorLogs.LastOrDefault()}");
            }
            catch (GarminClientException ex)
            {
                logger.LogError(ex, "GarminClientException occurred. AuthStatus: {AuthStatus}", ex.AuthStatus);
                return Results.BadRequest($"AuthStatus: {ex.AuthStatus}, message: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception occurred during /uploadbloodpressure.");
                return Results.BadRequest($"Exception: {ex.Message}");
            }
        });
    }
}
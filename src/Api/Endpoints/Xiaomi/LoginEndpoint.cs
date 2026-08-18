using Microsoft.Extensions.Caching.Memory;
using YetAnotherXiaomiCloudClient;

namespace Api.Endpoints.Xiaomi
{
    public record LoginSessionResponse(
       string SessionId,
       string LoginUrl,
       string QrCodeBase64,
       string PollingEndpoint
   );

    public record QrLoginStatusResponse(
        string Status,
        long? UserId = null,
        string? PassToken = null,
        string? CUserId = null,
        string? ServiceToken = null
    );

    public static class LoginEndpoint
    {
        private const int SESSION_TIMEOUT_MINUTES = 10;

        public static void MapXiaomiLoginEndpoint(this WebApplication app)
        {
            app.MapPost("xiaomi/login", PostLogin)
                .WithName("PostLogin")
                .Produces<LoginSessionResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            app.MapGet("xiaomi/login/{sessionId}", GetLoginStatus)
                .WithName("GetLoginStatus")
                .Produces<QrLoginStatusResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        private static async Task<IResult> PostLogin(IMemoryCache cache)
        {
            try
            {
                var authorization = new XiaomiClientAuthorization();

                // Execute steps 1-2: Get QR code
                if (!await authorization.LoginStep1Async())
                {
                    var errorResponse = new { error = "Unable to initiate QR code login" };
                    return Results.Json(errorResponse, statusCode: StatusCodes.Status500InternalServerError);
                }

                var qrCodeBytes = await authorization.LoginStep2Async();
                if (qrCodeBytes == null || qrCodeBytes.Length == 0)
                {
                    var errorResponse = new { error = "Unable to fetch QR code" };
                    return Results.Json(errorResponse, statusCode: StatusCodes.Status500InternalServerError);
                }

                // Generate session ID
                var sessionId = Guid.NewGuid().ToString();

                // Store authorization in cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(SESSION_TIMEOUT_MINUTES));

                cache.Set(sessionId, authorization, cacheOptions);

                // Convert QR code bytes to base64
                var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

                var response = new LoginSessionResponse(
                    SessionId: sessionId,
                    LoginUrl: authorization.LoginUrl,
                    QrCodeBase64: qrCodeBase64,
                    PollingEndpoint: $"/login/{sessionId}"
                );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = "An error occurred during login initialization", details = ex.Message };
                return Results.Json(errorResponse, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<IResult> GetLoginStatus(string sessionId, IMemoryCache cache)
        {
            try
            {
                // Retrieve authorization from cache
                if (!cache.TryGetValue(sessionId, out XiaomiClientAuthorization? authorization) || authorization == null)
                {
                    return Results.NotFound(new { error = "Session not found or expired" });
                }

                // Check if already logged in
                if (authorization.PassToken != null && authorization.UserId > 0)
                {
                    var completedResponse = new QrLoginStatusResponse(
                        Status: "completed",
                        UserId: authorization.UserId,
                        PassToken: authorization.PassToken,
                        CUserId: authorization.CUserId,
                        ServiceToken: authorization.ServiceToken
                    );

                    // Remove from cache after successful completion
                    cache.Remove(sessionId);

                    return Results.Ok(completedResponse);
                }

                // Execute steps 3-4: Poll for result and get service token
                var step3Result = await authorization.LoginStep3Async();

                if (step3Result)
                {
                    var step4Result = await authorization.LoginStep4Async();

                    if (step4Result)
                    {
                        var completedResponse = new QrLoginStatusResponse(
                            Status: "completed",
                            UserId: authorization.UserId,
                            PassToken: authorization.PassToken,
                            CUserId: authorization.CUserId,
                            ServiceToken: authorization.ServiceToken
                        );

                        // Remove from cache after successful completion
                        cache.Remove(sessionId);

                        return Results.Ok(completedResponse);
                    }
                }

                // Still pending
                var pendingResponse = new QrLoginStatusResponse(Status: "pending");
                return Results.Ok(pendingResponse);
            }
            catch (InvalidOperationException ex)
            {
                // QR code not scanned yet or timeout
                var pendingResponse = new QrLoginStatusResponse(Status: "pending");
                return Results.Ok(pendingResponse);
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = "An error occurred while checking login status", details = ex.Message };
                return Results.Json(errorResponse, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}

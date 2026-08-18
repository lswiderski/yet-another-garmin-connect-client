using YetAnotherXiaomiCloudClient;

namespace Api.Endpoints.Xiaomi
{
    public record PostWeightsRequest(long UserId, string PassToken, string Region, string Model);

    public static class WeightsEndpoint
    {
        public static void MapXiaomiWeightsEndpoint(this WebApplication app)
        {
            app.MapPost("xiaomi/weights", GetWeights)
                .WithName("GetWeights")
                .Produces<List<Weight>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        private static async Task<IResult> GetWeights(PostWeightsRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Results.BadRequest(new { error = "Request body is required" });
                }

                if (string.IsNullOrWhiteSpace(request.PassToken))
                {
                    return Results.BadRequest(new { error = "PassToken is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Region))
                {
                    return Results.BadRequest(new { error = "Region is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Model))
                {
                    return Results.BadRequest(new { error = "Model is required" });
                }

                var client = new XiaomiClient("xiaomiio");

                await client.LoginWithToken(request.UserId, request.PassToken);

                var weights = await client.GetModelWeights(request.Region, request.Model);

                return Results.Ok(weights);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = "An error occurred while retrieving weights", details = ex.Message };
                return Results.Json(errorResponse, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}

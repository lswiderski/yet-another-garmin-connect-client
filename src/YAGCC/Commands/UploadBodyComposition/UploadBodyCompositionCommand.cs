using Spectre.Console.Cli;
using Spectre.Console;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient;

namespace YAGCC.Commands.UploadBodyComposition
{
    public class UploadBodyCompositionCommand : Command<UploadBodyCompositionCommandSettings>
    {
        public override int Execute(CommandContext context, UploadBodyCompositionCommandSettings settings)
        {
            var result = Upload(settings).Result;
            AnsiConsole.MarkupLine(result);
            if (result == "Uploaded")
            {
                return 0;
            }
            else
            {
                return -1;
            }

        }

        private async Task<string> Upload(UploadBodyCompositionCommandSettings request)
        {
            try
            {
                var garminClient = await ClientFactory.Create();
                var userProfileSettings = new UserProfileSettings
                {
                    Age = 40,
                    Height = 180,
                };
                var garminWeightScaleDTO = new GarminWeightScaleDTO
                {
                    TimeStamp = request.TimeStamp.HasValue ? DateTime.UnixEpoch.AddSeconds(request.TimeStamp.Value) : DateTime.UtcNow,
                    Weight = request.Weight,
                    PercentFat = request.PercentFat,
                    PercentHydration = request.PercentHydration,
                    BoneMass = request.BoneMass,
                    MuscleMass = request.MuscleMass,
                    VisceralFatRating = request.VisceralFatRating.HasValue ? (byte)Math.Round(request.VisceralFatRating.Value) : null,
                    VisceralFatMass = request.VisceralFatMass,
                    PhysiqueRating = request.PhysiqueRating.HasValue ? (byte)Math.Round(request.PhysiqueRating.Value) : null,
                    MetabolicAge = request.MetabolicAge.HasValue ? (byte)Math.Round(request.MetabolicAge.Value) : null,
                    BodyMassIndex = request.BodyMassIndex,
                };

                var credentials = new CredentialsData
                {
                    Email = request.Email,
                    Password = request.Password,
                };

                var uploadResult = await garminClient.UploadWeight(garminWeightScaleDTO, userProfileSettings, credentials);

                if (uploadResult.IsSuccess)
                {
                    return "Uploaded";
                }
                return $"AuthStatus: {uploadResult.AuthStatus}, Last Error log: {uploadResult.ErrorLogs.LastOrDefault()}";

            }
            catch (GarminClientException ex)
            {
                return $"AuthStatus: {ex.AuthStatus}, message: {ex.Message}";
            }
            catch (Exception ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
                return $"Last Error log: {errorLogs.LastOrDefault()}, message: {ex.Message}";
            }
        }
    }
}

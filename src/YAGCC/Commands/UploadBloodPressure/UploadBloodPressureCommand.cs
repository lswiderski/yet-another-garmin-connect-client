using Spectre.Console.Cli;
using Spectre.Console;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient;

namespace YAGCC.Commands.UploadBloodPressure
{
    public class UploadBloodPressureCommand : Command<UploadBloodPressureCommandSettings>
    {
        public override int Execute(CommandContext context, UploadBloodPressureCommandSettings settings)
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

        private async Task<string> Upload(UploadBloodPressureCommandSettings request)
        {
            try
            {
                var garminClient = await ClientFactory.Create();

                var bloodDto = new BloodPressureDataDTO
                {
                    TimeStamp = request.TimeStamp.HasValue ? DateTime.UnixEpoch.AddSeconds(request.TimeStamp.Value) : DateTime.UtcNow,
                    HeartRate = request.HeartRate,
                    SystolicPressure = request.SystolicPressure,
                    DiastolicPressure = request.DiastolicPressure,
                    Email = request.Email,
                    Password = request.Password,
                };

                var uploadResult = await garminClient.UploadBlood(bloodDto);

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


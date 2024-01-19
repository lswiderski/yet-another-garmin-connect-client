using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace YetAnotherGarminConnectClient
{
    internal partial class Client : IClient
    {
        public async Task<UploadResult> UploadWeight(GarminWeightScaleDTO weightScaleDTO, UserProfileSettings userProfileSettings, string? mfaCode = "")
        {
            var result = await TryToAuthenticate(weightScaleDTO.Email, weightScaleDTO.Password, mfaCode);

            if (IsOAuthValid)
            {
                byte[] file = null;
                try
                {
                    file = FitFileCreator.CreateWeightBodyCompositionFitFile(weightScaleDTO, userProfileSettings);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Problem with creating fit file");
                }

                result = await TryToUploadActivity(result, file);

            }

            return result;
        }

    }
}

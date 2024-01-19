using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;
using YetAnotherGarminConnectClient.Dto;

namespace YetAnotherGarminConnectClient
{
    internal partial class Client : IClient
    {
        public async Task<UploadResult> UploadBlood(BloodPressureDataDTO bloodPressureData, string? mfaCode = "")
        {
            var result = await TryToAuthenticate(bloodPressureData.Email, bloodPressureData.Password, mfaCode);

            if (IsOAuthValid)
            {
                byte[] file = null;
                try
                {
                    file = FitFileCreator.CreateBloodPressureFitFile(bloodPressureData);
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

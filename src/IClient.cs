using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace YetAnotherGarminConnectClient
{
    public interface IClient
    {
        public OAuth2Token OAuth2Token { get; }
        Task SetOAuth2Token(string accessToken, string tokenSecret);
        Task<GarminAuthenciationResult> Authenticate(string email, string password);
        Task<GarminAuthenciationResult> CompleteMFAAuthAsync(string mfaCode);
        Task<UploadResponse> UploadActivity(string format, byte[] file);
        Task<WeightUploadResult> UploadWeight(GarminWeightScaleDTO weightScaleDTO, UserProfileSettings userProfileSettings, string? mfaCode = "");
    }
}

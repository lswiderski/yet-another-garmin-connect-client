using System.Text.Json;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace YetAnotherGarminConnectClient
{
    internal partial class Client : IClient
    {
        public async Task<UploadResult> UploadWeight(GarminWeightScaleDTO weightScaleDTO, UserProfileSettings userProfileSettings, CredentialsData credentials, string? mfaCode = "")
        {
            var result = new UploadResult();
            if (!string.IsNullOrEmpty(credentials.SerializedOAuth2Token)) {
                var deserializedToken = JsonSerializer.Deserialize<OAuth2Token>(credentials.SerializedOAuth2Token);
                OAuth2Token = deserializedToken;
                this._oAuth2TokenValidUntil = DateTime.UtcNow.AddSeconds(OAuth2Token.Expires_In);
            }
            else if(!string.IsNullOrEmpty(credentials.AccessToken) && !string.IsNullOrEmpty(credentials.TokenSecret))
            {
                await SetOAuth2Token(credentials.AccessToken, credentials.TokenSecret);
            }

            if (!IsOAuthValid)
            {
                result = await TryToAuthenticate(credentials.Email, credentials.Password, mfaCode);
            }

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

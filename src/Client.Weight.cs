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

            if(!string.IsNullOrEmpty(credentials.AccessToken) && !string.IsNullOrEmpty(credentials.TokenSecret) && string.IsNullOrEmpty(mfaCode))
            {
                await SetOAuth2Token(credentials.AccessToken, credentials.TokenSecret);

                if (OAuth2Token == null)
                {
                    _authStatus = AuthStatus.OAuthToken2IsNullFromSavedOAuth1;

                }
                else
                {
                    _authStatus = AuthStatus.Authenticated;
                }
                
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

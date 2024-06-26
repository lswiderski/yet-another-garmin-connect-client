﻿using System.Text.Json;
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

                result.AccessToken = this._oAuth1AccessToken;
                result.TokenSecret = this._oAuth1TokenSecret;

                _authStatus = OAuth2Token == null 
                    ? AuthStatus.OAuthToken2IsNullFromSavedOAuth1
                    : AuthStatus.Authenticated;
            }

            if (!IsOAuthValid)
            {
                result = await TryToAuthenticate(credentials.Email, credentials.Password, mfaCode);
            }

            if (IsOAuthValid)
            {
                byte[] file = this.GenerateWeightFitFile(weightScaleDTO, userProfileSettings);

                result = await TryToUploadActivity(result, file);
            }

            return result;
        }

        public byte[] GenerateWeightFitFile(GarminWeightScaleDTO weightScaleDTO, UserProfileSettings userProfileSettings)
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
            return file;
        }

    }
}

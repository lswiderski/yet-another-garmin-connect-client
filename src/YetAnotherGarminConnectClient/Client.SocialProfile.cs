using Flurl.Http;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin;
using YetAnotherGarminConnectClient.Dto.Garmin.Fit;

namespace YetAnotherGarminConnectClient
{
    internal partial class Client : IClient
    {

        public async Task<RequestResult> GetSocialProfile(CredentialsData credentials, string? mfaCode = "")
        {
            var result = new RequestResult();


            if (!string.IsNullOrEmpty(credentials.AccessToken) && !string.IsNullOrEmpty(credentials.TokenSecret) && string.IsNullOrEmpty(mfaCode))
            {
                await SetOAuth2Token(credentials.AccessToken, credentials.TokenSecret);

                _authStatus = OAuth2Token == null
                    ? AuthStatus.OAuthToken2IsNullFromSavedOAuth1
                    : AuthStatus.Authenticated;
            }

            if (!IsOAuthValid)
            {
                await TryToAuthenticate(credentials.Email, credentials.Password, mfaCode);
            }

            if (IsOAuthValid)
            {
                try
                {

                    var response = await URLs.SOCIAL_PROFILE_URL(_domain)
                        .WithOAuthBearerToken(OAuth2Token.Access_Token)
                     .WithHeader("NK", "NT")
                     .WithHeader("origin", URLs.ORIGIN(_domain))
                     .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                     .AllowHttpStatus("2xx,409")
                    .GetJsonAsync<SocialProfileResponse>();


                    result.IsSuccess = !string.IsNullOrEmpty(response.GarminGUID);
                    result.FullName = response.UserProfileFullName!;
                    result.AccessToken = this._oAuth1AccessToken;
                    result.TokenSecret = this._oAuth1TokenSecret;
                }


                catch (FlurlHttpException ex)
                {
                    this._logger.Error(ex, "Failed to get social profile from Garmin. Flur Exception.");
                }
                catch (Exception ex)
                {
                    this._logger.Error(ex, "Failed to get social profile from Garmin.");
                }
            }
            return result;
        }
    }
}

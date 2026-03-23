using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin;

namespace YetAnotherGarminConnectClient
{
    internal partial class Client : IClient
    {
        private CookieJar _mobileSessionCookies = null;

        public async Task<GarminAuthenciationResult> Authenticate(string email, string password)
        {
            var result = new GarminAuthenciationResult();

            _authStatus = AuthStatus.PreInitCookies;
            try
            {
                // Step 1: Initialize mobile session
                await InitMobileSessionAsync();
            }
            catch (FlurlHttpException ex)
            {
                _authStatus = AuthStatus.InitCookiesError;
                this._logger.Error(ex, "Failed to initialize mobile session");
                throw new GarminClientException(_authStatus, ex.Message, ex);
            }

            _authStatus = AuthStatus.InitCookiesSuccessful;

            // Step 2: Login via mobile API
            var loginData = new
            {
                username = email,
                password = password,
                rememberMe = false,
                captchaToken = ""
            };

            LoginResponse loginResponse = null;
            try
            {
                loginResponse = await this.SendMobileLoginAsync(loginData);
            }
            catch (FlurlHttpException ex) when (ex.StatusCode is (int)HttpStatusCode.TooManyRequests)
            {
                _authStatus = AuthStatus.AuthBlockedByCloudFlare;
                var errorMessage = "Garmin Authentication Failed. Rate limited (429).";
                this._logger.Error(ex, errorMessage);
                throw new GarminClientException(_authStatus, errorMessage, ex);
            }
            catch (FlurlHttpException ex)
            {
                _authStatus = AuthStatus.AuthenticationFailedCheckCredencials;
                this._logger.Error(ex, "Mobile login failed.");
                throw new GarminClientException(_authStatus, ex.Message, ex);
            }

            // Step 3: Check response status
            var responseStatus = loginResponse?.ResponseStatus?.Type ?? string.Empty;

            if (responseStatus == "MFA_REQUIRED")
            {
                result.MFACodeRequested = true;
                _authStatus = AuthStatus.MFARedirected;
                return result;
            }

            if (responseStatus == "INVALID_USERNAME_PASSWORD")
            {
                _authStatus = AuthStatus.AuthenticationFailedCheckCredencials;
                var errorMessage = "Invalid username or password";
                this._logger.Error(errorMessage);
                throw new GarminClientException(_authStatus, errorMessage);
            }

            if (responseStatus != "SUCCESSFUL")
            {
                _authStatus = AuthStatus.AuthenticationFailed;
                var errorMessage = $"Unhandled Garmin Login response: {responseStatus}";
                this._logger.Error(errorMessage);
                throw new GarminClientException(_authStatus, errorMessage);
            }

            // Step 4: Extract service ticket and establish web session
            var serviceTicket = loginResponse?.ServiceTicketId;
            if (string.IsNullOrEmpty(serviceTicket))
            {
                _authStatus = AuthStatus.SuccessButCouldNotFindServiceTicket;
                var errorMessage = "Auth successful but no service ticket found";
                this._logger.Error(errorMessage);
                throw new GarminClientException(_authStatus, errorMessage);
            }

            try
            {
                await EstablishWebSessionAsync(serviceTicket);
            }
            catch (FlurlHttpException ex)
            {
                _authStatus = AuthStatus.OAuthToken2IsNull;
                this._logger.Error(ex, "Failed to establish web session");
                throw new GarminClientException(_authStatus, ex.Message, ex);
            }

            if (OAuth2Token == null)
            {
                _authStatus = AuthStatus.OAuthToken2IsNull;
                var errorMessage = "Failed to get JWT token";
                throw new GarminClientException(_authStatus, errorMessage);
            }

            _authStatus = AuthStatus.Authenticated;
            result.IsSuccess = true;
            return result;
        }

        public async Task<GarminAuthenciationResult> CompleteMFAAuthAsync(string mfaCode)
        {
            if (_authStatus != AuthStatus.MFARedirected)
            {
                var message = "Not in MFA state";
                this._logger.Error(message);
                throw new GarminClientException(_authStatus, message);
            }

            var mfaData = new
            {
                mfaMethod = "email",
                mfaVerificationCode = mfaCode,
                rememberMyBrowser = false,
                reconsentList = new List<string>(),
                mfaSetup = false
            };

            try
            {
                var mfaResponse = await SendMfaCodeAsync(mfaData);

                var responseStatus = mfaResponse?.ResponseStatus?.Type ?? string.Empty;

                if (responseStatus != "SUCCESSFUL")
                {
                    _authStatus = AuthStatus.InvalidMFACode;
                    this._logger.Error($"MFA verification failed: {responseStatus}");
                    throw new GarminClientException(_authStatus, "Invalid MFA code");
                }

                var serviceTicket = mfaResponse?.ServiceTicketId;
                if (string.IsNullOrEmpty(serviceTicket))
                {
                    _authStatus = AuthStatus.SuccessButCouldNotFindServiceTicket;
                    throw new GarminClientException(_authStatus, "No service ticket after MFA");
                }

                await EstablishWebSessionAsync(serviceTicket);

                if (OAuth2Token == null)
                {
                    _authStatus = AuthStatus.OAuthToken2IsNull;
                    throw new GarminClientException(_authStatus, "Failed to get JWT token after MFA");
                }

                _authStatus = AuthStatus.Authenticated;
                var result = new GarminAuthenciationResult { IsSuccess = true };
                return result;
            }
            catch (FlurlHttpException ex) when (ex.StatusCode is (int)HttpStatusCode.TooManyRequests)
            {
                _authStatus = AuthStatus.MFAAuthBlockedByCloudFlare;
                var errorMessage = "MFA blocked by rate limit (429).";
                this._logger.Error(ex, errorMessage);
                throw new GarminClientException(_authStatus, errorMessage, ex);
            }
        }


        private async Task InitMobileSessionAsync()
        {
            // Initialize mobile session to get initial cookies
            _mobileSessionCookies = null;

            await URLs.SSO_MOBILE_SIGNIN_URL(_domain)
                        .WithHeader("User-Agent", MagicStrings.MOBILE_USER_AGENT)
                        .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
                        .WithHeader("Accept-Language", "en-US,en;q=0.9")
                        .SetQueryParam("clientId", "GarminConnect")
                        .WithCookies(out var jar)
                        .GetStringAsync();

            _mobileSessionCookies = jar;
        }

        private async Task<LoginResponse> SendMobileLoginAsync(object loginData)
        {
            try
            {
                var response = await URLs.SSO_MOBILE_LOGIN_URL(_domain)
                            .WithHeader("User-Agent", MagicStrings.MOBILE_USER_AGENT)
                            .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
                            .WithHeader("Accept-Language", "en-US,en;q=0.9")
                            .SetQueryParams(new { 
                                clientId = "GarminConnect",
                                locale = "en-US",
                                service = "https://connect.garmin.com/app/"
                            })
                            .WithCookies(_mobileSessionCookies)
                            .PostJsonAsync(loginData)
                            .ReceiveJson<LoginResponse>();

                return response;
            }
            catch (FlurlHttpException ex)
            {
                _logger.Error($"Error during mobile login: {ex.Message}");
                throw;
            }
        }

        private async Task<LoginResponse> SendMfaCodeAsync(object mfaData)
        {
            try
            {
                var response = await URLs.SSO_MOBILE_MFA_URL(_domain)
                            .WithHeader("User-Agent", MagicStrings.MOBILE_USER_AGENT)
                            .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
                            .WithHeader("Accept-Language", "en-US,en;q=0.9")
                            .SetQueryParams(new { 
                                clientId = "GarminConnect",
                                locale = "en-US",
                                service = "https://connect.garmin.com/app/"
                            })
                            .WithCookies(_mobileSessionCookies)
                            .PostJsonAsync(mfaData)
                            .ReceiveJson<LoginResponse>();

                return response;
            }
            catch (FlurlHttpException ex)
            {
                _logger.Error($"Error during MFA submission: {ex.Message}");
                throw;
            }
        }

        private async Task EstablishWebSessionAsync(string serviceTicket)
        {
            // Step 1: Use service ticket to establish web session
            _cookieJar = null;

            await "https://connect.garmin.com/app/"
                        .WithHeader("User-Agent", MagicStrings.WEB_USER_AGENT)
                        .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8")
                        .WithHeader("Accept-Language", "en-US,en;q=0.9")
                        .SetQueryParam("ticket", serviceTicket)
                        .WithCookies(out var jar)
                        .GetStringAsync();

            _cookieJar = jar;

            // Step 2: Refresh JWT token
            await RefreshJWTTokenAsync();
        }

        private async Task RefreshJWTTokenAsync()
        {
            try
            {
                var jwtResponse = await $"https://connect.garmin.com/services/auth/token/di-oauth/refresh"
                            .WithHeader("User-Agent", MagicStrings.WEB_USER_AGENT)
                            .WithHeader("Accept", "application/json")
                            .WithHeader("NK", "NT")
                            .WithHeader("Referer", "https://connect.garmin.com/modern/")
                            .WithCookies(_cookieJar)
                            .PostAsync(null)
                            .ReceiveJson<JWTTokenResponse>();

                if (string.IsNullOrEmpty(jwtResponse?.EncryptedToken))
                {
                    throw new Exception("No encrypted token in response");
                }

                OAuth2Token = new OAuth2Token
                {
                    Access_Token = jwtResponse.EncryptedToken,
                    Token_Type = "Bearer",
                    Expires_In = 3600,
                    Scope = "fullAccess"
                };

                _CsrfToken = jwtResponse.CsrfToken;

                this._oAuth2TokenValidUntil = DateTime.UtcNow.AddSeconds(3600);
                this._oAuth1AccessToken = jwtResponse.EncryptedToken;
            }
            catch (FlurlHttpException ex)
            {
                _logger.Error($"Error during JWT refresh: {ex.Message}");
                throw;
            }
        }

        public async Task SetJWTToken(string accessToken)
        {
            OAuth2Token = new OAuth2Token
            {
                Access_Token = accessToken,
                Token_Type = "Bearer",
                Expires_In = 3600,
                Scope = "fullAccess"
            };

            if (OAuth2Token != null)
            {
                this._oAuth2TokenValidUntil = DateTime.UtcNow.AddSeconds(OAuth2Token.Expires_In);
                this._oAuth1AccessToken = accessToken;
            }
        }

        public async Task SetOAuth2Token(string accessToken, string tokenSecret)
        {
            // For backward compatibility, this now simply sets the JWT token
            await SetJWTToken(accessToken);
        }
    }
}

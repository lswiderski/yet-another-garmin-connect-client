using Flurl.Http;
using OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using YetAnotherGarminConnectClient.Dto;
using YetAnotherGarminConnectClient.Dto.Garmin;

namespace YetAnotherGarminConnectClient
{
    internal partial class Client : IClient
    {


        public async Task<GarminAuthenciationResult> Authenticate(string email, string password)
        {
            var result = new GarminAuthenciationResult();
            _cookieJar = null;

            // Set cookies
            try
            {
                await this.InitCookieJarAsync();
            }


            catch (FlurlHttpException ex)
            {
                this._logger.Error(ex, "Failed on first step of authentication flow. Init Cookies");
                result.IsSuccess = false;
                result.Error = ex.Message;
                return result;
            }

            // get CSRF token
            var csrfRequest = new
            {
                id = "gauth-widget",
                embedWidget = "true",
                gauthHost = URLs.SSO_EMBED_URL,
                service = URLs.SSO_EMBED_URL,
                source = URLs.SSO_EMBED_URL,
                redirectAfterAccountLoginUrl = URLs.SSO_EMBED_URL,
                redirectAfterAccountCreationUrl = URLs.SSO_EMBED_URL,
            };

            string csrfToken = string.Empty;
            try
            {
                var tokenResult = await this.GetCsrfTokenAsync(csrfRequest);
                csrfToken = this.FindCsrfToken(tokenResult);
            }
            catch (FlurlHttpException ex)
            {
                this._logger.Error(ex, "Failed to fetch csrf token from Garmin.");
                result.IsSuccess = false;
                result.Error = ex.Message;
                return result;
            }

            // send credencials
            var sendCredentialsRequest = new
            {
                username = email,
                password = password,
                embed = "true",
                _csrf = csrfToken
            };

            SendCredentialsResult sendCredentialsResult = null;
            try
            {
                sendCredentialsResult = await this.SendCredentialsAsync(csrfRequest, sendCredentialsRequest);
            }
            catch (FlurlHttpException ex) when (ex.StatusCode is (int)HttpStatusCode.Forbidden)
            {
                var responseContent = (await ex.GetResponseStringAsync()) ?? string.Empty;

                if (responseContent == "error code: 1020")
                {
                    var errorMessage = "Garmin Authentication Failed. Blocked by CloudFlare.";
                    this._logger.Error(ex, errorMessage);
                    result.IsSuccess = false;
                    result.Error = errorMessage;
                    return result;
                }
                this._logger.Error(ex, "Garmin Authentication Failed.");
                result.IsSuccess = false;
                result.Error = ex.Message;
                return result;
            }

            var loginResult = sendCredentialsResult?.RawResponseBody;

            var ticketRegex = new Regex(MagicStrings.TICKET_REGEX);
            var ticketMatch = ticketRegex.Match(loginResult);
            if (!ticketMatch.Success)
            {
                var errorMessage = "Auth appeared successful but failed to find regex match for service ticket.";
                this._logger.Error(errorMessage);
                result.IsSuccess = false;
                result.Error = errorMessage;
                return result;
            }

            var ticket = ticketMatch.Groups.GetValueOrDefault("ticket").Value;
            _logger.Debug($"Ticket: {ticket}");

            if (string.IsNullOrWhiteSpace(ticket))
            {
                var errorMessage = "Auth appeared successful, and found service ticket, but ticket was null or empty.";
                this._logger.Error(errorMessage);
                result.IsSuccess = false;
                result.Error = errorMessage;
                return result;
            }


            // get Oauth1
            var oAuth1 = await GetOAuth1Async(ticket);
            if (string.IsNullOrEmpty(oAuth1.oAuthToken) || string.IsNullOrEmpty(oAuth1.oAuthTokenSecret))
            {
                var errorMessage = "Auth appeared successful but failed to get the OAuth1 token.";
                result.IsSuccess = false;
                result.Error = errorMessage;
                return result;
            }

            // set OAuth2
            await SetOAuth2Token(oAuth1.oAuthToken, oAuth1.oAuthTokenSecret);

            if(OAuth2Token == null)
            {
                var errorMessage = "Auth appeared successful but failed to get the OAuth2 token.";
                result.IsSuccess = false;
                result.Error = errorMessage;
                return result;
            }

            result.IsSuccess = true;
            return result;
        }


        private async Task InitCookieJarAsync()
        {
            await URLs.SSO_EMBED_URL
                        .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                        .WithHeader("origin", URLs.ORIGIN)
                        .SetQueryParams(_commonQueryParams)
                        .WithCookies(out var jar)
                        .GetStringAsync();

            _cookieJar = jar;
        }

        public async Task<string> GetCsrfTokenAsync(object queryParams)
        {

            var result = await URLs.SSO_SIGNIN_URL
                        .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                        .WithHeader("origin", URLs.ORIGIN)
                        .SetQueryParams(queryParams)
                        .WithCookies(_cookieJar)
                        .GetAsync()
                        .ReceiveString();

            return result;
        }
        private async Task<SendCredentialsResult> SendCredentialsAsync(object queryParams, object loginData)
        {
            var result = new SendCredentialsResult();
            result.RawResponseBody = await URLs.SSO_SIGNIN_URL
                        .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                        .WithHeader("origin", URLs.ORIGIN)
                        .WithHeader("referer", URLs.REFERER)
                        .WithHeader("NK", "NT")
                        .SetQueryParams(queryParams)
                        .WithCookies(_cookieJar)
                        .OnRedirect((r) => { result.WasRedirected = true; result.RedirectedTo = r.Redirect.Url; })
                        .PostUrlEncodedAsync(loginData)
                        .ReceiveString();

            return result;
        }

        private async Task<(string oAuthToken, string oAuthTokenSecret)> GetOAuth1Async(string ticket)
        {

            OAuthRequest oauthRequest = OAuthRequest.ForRequestToken(_consumerKey, _consumerSecret);
            oauthRequest.RequestUrl = URLs.OAUTH1_URL(ticket);
            string authHeader = oauthRequest.GetAuthorizationHeader();
            try
            {
                var tokens = await oauthRequest.RequestUrl
                              .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                              .WithHeader("Authorization", authHeader)
                              .GetStringAsync();

                var splittedTokens = HttpUtility.ParseQueryString(tokens);

                if (splittedTokens.Count < 2)
                    throw new Exception($"OAuth1 response length did not match expected: {tokens.Length}");

                var oAuthToken = splittedTokens.Get("oauth_token");
                var oAuthTokenSecret = splittedTokens.Get("oauth_token_secret");

                if (string.IsNullOrWhiteSpace(oAuthToken))
                    throw new Exception("OAuth1 token is null");

                if (string.IsNullOrWhiteSpace(oAuthTokenSecret))
                    throw new Exception("OAuth1 token secret is null");

                return (oAuthToken, oAuthTokenSecret);
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseStringAsync();
                _logger.Error($"Error during OAuth1 handling, returned from {ex.Call.Request.Url}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during OAuth1 handling: {ex.Message}");
            }

            return ("", "");

        }

        public async Task SetOAuth2Token(string accessToken, string tokenSecret)
        {
            OAuth2Token = await this.GetOAuth2Token(accessToken, tokenSecret);

            if(OAuth2Token != null)
            {
                this._oAuth2TokenValidUntil = DateTime.UtcNow.AddSeconds(OAuth2Token.Expires_In);
            }
        }

        private async Task<OAuth2Token?> GetOAuth2Token(string accessToken, string tokenSecret)
        {

            OAuthRequest oauthRequest = OAuthRequest.ForProtectedResource("POST", _consumerKey, _consumerSecret, accessToken, tokenSecret);

            oauthRequest.RequestUrl = URLs.OAUTH_EXCHANGE_URL;
            string authHeader = oauthRequest.GetAuthorizationHeader();

            try
            {
                var token = await oauthRequest.RequestUrl
              .WithHeader("User-Agent", MagicStrings.USER_AGENT)
              .WithHeader("Authorization", authHeader)
              .WithHeader("Content-Type", "application/x-www-form-urlencoded")
              .WithTimeout(10)
              .PostUrlEncodedAsync(new object())
              .ReceiveJson<OAuth2Token>();

                return token;
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseStringAsync();
                _logger.Error($"Error during OAuth2 handling, returned from {ex.Call.Request.Url}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during OAuth2 handling: {ex.Message}");
            }

            return null;

        }

        private string FindCsrfToken(string rawResponseBody)
        {
            try
            {
                var tokenRegex = new Regex(MagicStrings.CSRF_REGEX);
                var match = tokenRegex.Match(rawResponseBody);
                if (!match.Success)
                    throw new Exception($"Failed to find regex match for csrf token. tokenResult: {rawResponseBody}");

                var csrfToken = match.Groups.GetValueOrDefault("csrf")?.Value;
                _logger.Debug($"Csrf Token: {csrfToken}");

                if (string.IsNullOrWhiteSpace(csrfToken))
                    throw new Exception("Found csrfToken but its null.");

                return csrfToken;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse csrf token.", e);
            }
        }
    }
}

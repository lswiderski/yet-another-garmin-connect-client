using Flurl.Http;
using OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using YetAnotherGarminConnectClient.Dto;

namespace YetAnotherGarminConnectClient
{
    internal partial class Client : IClient
    {


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
    }
}

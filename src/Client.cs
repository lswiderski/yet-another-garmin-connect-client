using Flurl.Http;
using NLog;
using OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto;

namespace YetAnotherGarminConnectClient
{
    internal class Client : IClient
    {
        private static string USER_AGENT = "com.garmin.android.apps.connectmobile"; 
        private readonly string _consumerKey;
        private readonly string _consumerSecret;

        private ILogger _logger => NLog.LogManager.GetLogger("Client");
        public OAuth2Token OAuth2Token { get; private set; }
        private Client() { }
        internal Client(string consumerKey, string consumerSecret)
        {

            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;

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
              .WithHeader("User-Agent", USER_AGENT)
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
            throw new Exception("Error during OAuth2 handling");

        }
    }
}

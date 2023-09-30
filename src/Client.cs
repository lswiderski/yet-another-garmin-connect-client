﻿using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
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
        private static string OAUTH_EXCHANGE_URL = "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0";
        private readonly string _consumerKey;
        private readonly string _consumerSecret;

        private readonly ILogger _logger;
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

            oauthRequest.RequestUrl = OAUTH_EXCHANGE_URL;
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
                _logger.LogError($"Error during OAuth2 handling, returned from {ex.Call.Request.Url}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during OAuth2 handling: {ex.Message}");
            }
            throw new Exception("Error during OAuth2 handling");

        }
    }
}

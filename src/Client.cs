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
    internal partial class Client : IClient
    {
        private readonly string _consumerKey;
        private readonly string _consumerSecret;
        private string _oAuthToken ="";
        private string _oAuthTokenSecret="";
        CookieJar _cookieJar = null;

        private static readonly object _commonQueryParams = new
        {
            id = "gauth-widget",
            embedWidget = "true",
            gauthHost = URLs.SSO_EMBED_URL,
            redirectAfterAccountCreationUrl = URLs.SSO_EMBED_URL,
            redirectAfterAccountLoginUrl = URLs.SSO_EMBED_URL,
            service = URLs.SSO_EMBED_URL,
            source = URLs.SSO_EMBED_URL,
        };

        private ILogger _logger => NLog.LogManager.GetLogger("Client");
        public OAuth2Token OAuth2Token { get; private set; }
        public DateTime _oAuth2TokenValidUntil { get; private set; }


        private Client() { }
        internal Client(string consumerKey, string consumerSecret)
        {

            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;

        }

        public bool IsOAuthValid
        {
            get
            {
                if (this.OAuth2Token == null)
                {
                    return false;
                }

                return DateTime.UtcNow < _oAuth2TokenValidUntil;
            }
        }

    }
}

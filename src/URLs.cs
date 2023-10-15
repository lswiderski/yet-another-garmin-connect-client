using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient
{
    public static class URLs
    {
        public static string GARMIN_API_CONSUMER_KEYS = "https://github.com/lswiderski/yet-another-garmin-connect-client/raw/main/oauth_consumer.json";
        public static string OAUTH_EXCHANGE_URL = "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0";
        public static string OAUTH1_URL(string ticket) => $"https://connectapi.garmin.com/oauth-service/oauth/preauthorized?ticket={ticket}&login-url=https://sso.garmin.com/sso/embed&accepts-mfa-tokens=true";
        public static string ORIGIN = "https://sso.garmin.com";
        public static string REFERER = "https://sso.garmin.com/sso/signin";
        public static string SSO_SIGNIN_URL = "https://sso.garmin.com/sso/signin";
        public static string SSO_EMBED_URL = "https://sso.garmin.com/sso/embed";
        public static string UPLOAD_URL = "https://connectapi.garmin.com/upload-service/upload";
    }
}

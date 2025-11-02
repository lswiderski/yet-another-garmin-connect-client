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
        public static string OAUTH_EXCHANGE_URL(string domain) => $"https://connectapi.{domain}/oauth-service/oauth/exchange/user/2.0";
        public static string OAUTH1_URL(string ticket, string domain) => $"https://connectapi.{domain}/oauth-service/oauth/preauthorized?ticket={ticket}&login-url=https://sso.{domain}/sso/embed&accepts-mfa-tokens=true";
        public static string ORIGIN(string domain) => $"https://sso.{domain}";
        public static string REFERER(string domain) => $"https://sso.{domain}/sso/signin";
        public static string SSO_SIGNIN_URL(string domain) => $"https://sso.{domain}/sso/signin";
        public static string SSO_EMBED_URL(string domain) => $"https://sso.{domain}/sso/embed";
        public static string UPLOAD_URL(string domain) => $"https://connectapi.{domain}/upload-service/upload";
        public static string SSO_ENTER_MFA_URL(string domain) => $"https://sso.{domain}/sso/verifyMFA/loginEnterMfaCode";
        public static string GARMIN_DOMAIN_GLOBAL = "garmin.com";
        public static string GARMIN_DOMAIN_CHINA = "garmin.cn";
    }
}

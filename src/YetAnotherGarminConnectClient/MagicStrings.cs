using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient
{
    public class MagicStrings
    {
        // Mobile User-Agent (Dalvik/Android) - used for initial authentication via mobile API
        public static string MOBILE_USER_AGENT = "Dalvik/2.1.0 (Linux; U; Android 13; Pixel 6 Build/TQ3A.230901.001) GarminConnect/4.74.1";

        // Web User-Agent - used for web session after obtaining service ticket
        public static string WEB_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        // Legacy property for backward compatibility
        public static string USER_AGENT = MOBILE_USER_AGENT;
        public static string CSRF_REGEX = "name=\"_csrf\"\\s+value=\"(?<csrf>.+?)\"";
        public static string TICKET_REGEX = "embed\\?ticket=(?<ticket>[^\"]+)\"";
    }
}

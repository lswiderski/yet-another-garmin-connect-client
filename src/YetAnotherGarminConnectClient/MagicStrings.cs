using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient
{
    public class MagicStrings
    {
        public static string USER_AGENT = "com.garmin.android.apps.connectmobile";
        public static string CSRF_REGEX = "name=\"_csrf\"\\s+value=\"(?<csrf>.+?)\"";
        public static string TICKET_REGEX = "embed\\?ticket=(?<ticket>[^\"]+)\"";
    }
}

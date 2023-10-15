using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient.Dto.Garmin
{
    public class SendCredentialsResult
    {
        public bool WasRedirected { get; set; }
        public string RedirectedTo { get; set; }
        public string RawResponseBody { get; set; }
    }
}

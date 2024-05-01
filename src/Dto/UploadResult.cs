using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient.Dto
{
    public class UploadResult
    {
        public bool IsSuccess { get; set; }
        public long UploadId { get; set; }
        public IList<string> Logs { get; set; }
        public IList<string> ErrorLogs { get; set; }
        public AuthStatus AuthStatus { get; set; }
        public bool MFACodeRequested { get; set; }
        public string? AccessToken { get; set; }
        public string? TokenSecret { get; set; }

    }
}

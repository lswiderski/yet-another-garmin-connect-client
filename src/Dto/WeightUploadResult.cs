using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient.Dto
{
    public class WeightUploadResult
    {
        public bool IsSuccess { get; set; }
        public long UploadId { get; set; }
        public IList<string> Logs { get; set; }
        public IList<string> ErrorLogs { get; set; }

    }
}

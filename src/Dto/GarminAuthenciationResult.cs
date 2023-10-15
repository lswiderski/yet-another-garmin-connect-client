using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient.Dto
{
    public class GarminAuthenciationResult
    {
        public bool IsSuccess { get; set; }
        public bool Response { get; set; }
        public string Error { get; set; }
    }
}

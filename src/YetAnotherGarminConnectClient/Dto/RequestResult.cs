using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient.Dto
{
    public class RequestResult
    {
        public bool IsSuccess { get; set; }
        public string FullName { get; set; }
        public string AccessToken { get; set; }
        public string TokenSecret { get; set; }
    }
}

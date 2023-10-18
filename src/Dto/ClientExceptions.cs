using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient.Dto
{

    public class GarminClientException : Exception
    {
        public AuthStatus AuthStatus { get; set; }

        public GarminClientException(AuthStatus curentStatus, string message) : base(message) {
            AuthStatus = curentStatus;
        }
        public GarminClientException(AuthStatus curentStatus, string message, Exception innerException) : base(message, innerException) {
            AuthStatus = curentStatus;
        }
    }
}

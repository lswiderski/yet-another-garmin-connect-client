using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient
{
    public class ClientFactory
    {
        public static IClient Create(string consumerKey, string consumerSecret)
        {
            var client = new Client(consumerKey, consumerSecret);
            return client;
        }
    }
}

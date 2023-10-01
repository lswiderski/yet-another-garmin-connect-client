using Flurl.Http;
using NLog;
using OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto;

namespace YetAnotherGarminConnectClient
{
    internal partial class Client : IClient
    {
        private readonly string _consumerKey;
        private readonly string _consumerSecret;
        private string _oAuthToken ="";
        private string _oAuthTokenSecret="";

        private ILogger _logger => NLog.LogManager.GetLogger("Client");
        public OAuth2Token OAuth2Token { get; private set; }
       
        private Client() { }
        internal Client(string consumerKey, string consumerSecret)
        {

            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;

        }

       
    }
}

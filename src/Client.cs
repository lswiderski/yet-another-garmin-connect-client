using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto;

namespace YetAnotherGarminConnectClient
{
    internal class Client : IClient
    {
        private readonly string _consumerKey;
        private readonly string _consumerSecret;
        private Client() { }
        internal Client(string consumerKey, string consumerSecret)
        {

            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;

        }
        public IClient Create(string consumerKey, string consumerSecret)
        {
            var client = new Client(consumerKey, consumerSecret);
            return client;
        }
        public async Task<OAuth2Token> GetOAuth2Token(string accessToken, string tokenSecret)
        {

            await Task.CompletedTask;
            return new OAuth2Token();
        }
    }
}

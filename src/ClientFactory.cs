using Flurl.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto;

namespace YetAnotherGarminConnectClient
{
    public class ClientFactory
    {

        private readonly ILogger _logger;
        public static IClient Create(string consumerKey, string consumerSecret)
        {
            var client = new Client(consumerKey, consumerSecret);
            return client;
        }

        public static async Task<IClient> Create()
        {
            var keys = await URLs.GARMIN_API_CONSUMER_KEYS
                            .GetAsync()
                            .ReceiveJson<GarminApiConsumerKeys>();

            if(keys == null)
            {
                throw new Exception($"Could not parse consumer keys from url: {URLs.GARMIN_API_CONSUMER_KEYS}");
            }

            var client = new Client(keys.ConsumerKey, keys.ConsumerSecret);
            return client;
        }
    }
}

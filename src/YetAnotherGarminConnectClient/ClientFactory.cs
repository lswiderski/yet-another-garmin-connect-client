using Flurl.Http;
using NLog;
using YetAnotherGarminConnectClient.Dto;

namespace YetAnotherGarminConnectClient
{
    public class ClientFactory
    {

        private static ILogger _logger => NLog.LogManager.GetLogger("ClientFactory");
        public static IClient Create(string domain, string consumerKey, string consumerSecret)
        {
            Logger.CreateLogger();
            var client = new Client(domain, consumerKey, consumerSecret);
            return client;
        }

        public static async Task<IClient> Create(GarminServer server = GarminServer.GLOBAL)
        {
            Logger.CreateLogger();
            var keys = await URLs.GARMIN_API_CONSUMER_KEYS
                            .GetAsync()
                            .ReceiveJson<GarminApiConsumerKeys>();

            if(keys == null)
            {
                _logger.Error($"Could not parse consumer keys from url: {URLs.GARMIN_API_CONSUMER_KEYS}");
                throw new Exception($"Could not parse consumer keys from url: {URLs.GARMIN_API_CONSUMER_KEYS}");
                
            }
            _logger.Info("Consumer Keys received");
            string domain = GetDomain(server);

            var client = Create(domain, keys.ConsumerKey, keys.ConsumerSecret);
            return client;
        }
        private static string GetDomain(GarminServer server)
        {
            return server switch
            {
                GarminServer.GLOBAL => URLs.GARMIN_DOMAIN_GLOBAL,
                GarminServer.CHINA => URLs.GARMIN_DOMAIN_CHINA,
                _ => URLs.GARMIN_DOMAIN_GLOBAL,
            };
        }


    }
}

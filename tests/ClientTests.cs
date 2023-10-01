using NLog.Targets;
using NLog;
using NUnit.Framework;

namespace YetAnotherGarminConnectClient.Tests
{
    public class Tests
    {
        private string _consumerKey = "";
        private string _consumerSecret = "";

        private IClient _client;
        [SetUp]
        public async Task Setup()
        {
            _client = await ClientFactory.Create();
        }

        [Test]
        public async Task ShouldReceiveOAuth2Token()
        {
            try
            {
                var accessToken = "";
                var tokenSecret = "";
                await _client.SetOAuth2Token(accessToken, tokenSecret);

            }
            catch (Exception ex)
            {
                var logs = Logger.GetLogs();
                var errorLogs = Logger.GetErrorLogs();
            }

            Assert.IsNotNull(_client.OAuth2Token);
        }

    }
}
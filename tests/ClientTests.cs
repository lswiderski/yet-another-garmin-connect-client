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
            var accessToken = "";
            var tokenSecret = "";
            await _client.SetOAuth2Token(accessToken, tokenSecret);
            Assert.IsNotNull(_client.OAuth2Token);
        }


    }
}
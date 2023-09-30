using NUnit.Framework;

namespace YetAnotherGarminConnectClient.Tests
{
    public class Tests
    {
        private string _consumerKey = "";
        private string _consumerSecret = "";

        private IClient _client;
        [SetUp]
        public void Setup()
        {
            _client = ClientFactory.Create(_consumerKey, _consumerSecret);
        }

        [Test]
        public void ShouldReceiveOAuth2Token()
        {
            var accessToken = "";
            var tokenSecret = "";
            var token = _client.GetOAuth2Token(accessToken, tokenSecret);
            Assert.IsNotNull(token);
        }
    }
}
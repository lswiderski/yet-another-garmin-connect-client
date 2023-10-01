using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient.Tests
{
    public class ClientFactoryTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task ShouldReceiveOAuth2Token()
        {
            Assert.DoesNotThrowAsync(() => ClientFactory.Create());
        }
    }
}

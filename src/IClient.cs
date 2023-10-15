using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherGarminConnectClient.Dto;

namespace YetAnotherGarminConnectClient
{
    public interface IClient
    {
        public OAuth2Token OAuth2Token { get; }
        Task SetOAuth2Token(string accessToken, string tokenSecret);
        Task<GarminAuthenciationResult> Authenticate(string email, string password);
    }
}

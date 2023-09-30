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
        Task<OAuth2Token> GetOAuth2Token(string accessToken, string tokenSecret);
    }
}

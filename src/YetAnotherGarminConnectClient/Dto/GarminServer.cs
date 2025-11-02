using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient.Dto
{
    public enum GarminServer
    {
        GLOBAL = 0,
        CHINA = 1,
    }
    public static class GarminServerHelper
    {
        public static GarminServer GetServer(string server)
        {
            return server.ToLower() switch
            {
                "china" => GarminServer.CHINA,
                _ => GarminServer.GLOBAL,
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace YetAnotherGarminConnectClient.Dto
{
    public class GarminApiConsumerKeys
    {
        [JsonPropertyName("consumer_key")]
        public string ConsumerKey { get; set; }

        [JsonPropertyName("consumer_secret")]
        public string ConsumerSecret { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace YetAnotherGarminConnectClient.Dto
{
    public class LoginResponse
    {
        [JsonPropertyName("responseStatus")]
        public ResponseStatus ResponseStatus { get; set; }

        [JsonPropertyName("serviceTicketId")]
        public string ServiceTicketId { get; set; }

        [JsonPropertyName("customerMfaInfo")]
        public CustomerMfaInfo CustomerMfaInfo { get; set; }
    }

    public class ResponseStatus
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class CustomerMfaInfo
    {
        [JsonPropertyName("mfaLastMethodUsed")]
        public string MfaLastMethodUsed { get; set; }
    }

    public class JWTTokenResponse
    {
        [JsonPropertyName("encryptedToken")]
        public string EncryptedToken { get; set; }

        [JsonPropertyName("csrfToken")]
        public string CsrfToken { get; set; }

        [JsonPropertyName("signedInUser")]
        public string SignedInUser { get; set; }
    }
}

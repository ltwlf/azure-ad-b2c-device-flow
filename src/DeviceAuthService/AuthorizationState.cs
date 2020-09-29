using Newtonsoft.Json;

namespace Ltwlf.Azure.B2C
{
    [JsonObject]
    public class AuthorizationState
    {
        [JsonProperty("client_id")] public string ClientId { get; set; }

        [JsonProperty("device_code")] public string DeviceCode { get; set; }

        [JsonProperty("user_code")] public string UserCode { get; set; }

        [JsonProperty("verification_uri")] public string VerificationUri { get; set; }

        [JsonProperty("expires_in")] public int ExpiresIn { get; set; }

        [JsonProperty("token")] public string Token { get; set; }
    }
}
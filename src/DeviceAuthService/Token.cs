using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Ltwlf.Azure.B2C
{
    public class Token
    {
        private class TokenResponse
        {
            [JsonProperty("access_Token")] public string AccessToken { get; set; }
            [JsonProperty("token_type")] public string TokenType { get; set; }
            [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
            [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
            
            [JsonProperty("scope")] public string Scope { get; set; }
        }

        private readonly IConnectionMultiplexer _muxer;

        public Token(IConnectionMultiplexer muxer)
        {
            _muxer = muxer;
        }

        [FunctionName("Token")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequest req, ILogger log)
        {
            var deviceCode = req.Form["device_code"].SingleOrDefault();
            var grantType = req.Form["grant_type"].SingleOrDefault();
            var clientId = req.Form["client_id"].SingleOrDefault();

            if (deviceCode == null || grantType == null || clientId == null)
                return new BadRequestObjectResult("device_code, client_id, grant_type are mandatory");

            if (!grantType.Equals("urn:ietf:params:oauth:grant-type:device_code", StringComparison.OrdinalIgnoreCase))
                return new BadRequestObjectResult("gran_type must be \"urn:ietf:params:oauth:grant-type:device_code\"");

            var pattern = $"{deviceCode}:*";

            var key = await Helpers.GetKeyByPattern(_muxer, pattern);
            if (key == null) return new BadRequestObjectResult("expired_token");

            var value = _muxer.GetDatabase().StringGet(key);
            var authStateJson = value.ToString();

            var authState = JsonConvert.DeserializeObject<AuthorizationState>(authStateJson);

            if (authState.AccessToken == null) return new BadRequestObjectResult("authorization_pending");

            var tokenResponse = new TokenResponse
            {
                AccessToken = authState.AccessToken,
                RefreshToken = authState.RefreshToken,
                ExpiresIn = authState.ExpiresIn,
                TokenType = authState.TokenType,
                Scope =  authState.Scope
            };

            return new OkObjectResult(tokenResponse);
        }
    }
}
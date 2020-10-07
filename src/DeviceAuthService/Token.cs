using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Ltwlf.Azure.B2C
{
    public class Token
    {
        private class TokenResponse
        {
            [JsonProperty("access_token")] public string AccessToken { get; set; }
            [JsonProperty("token_type")] public string TokenType { get; set; }
            [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
            [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
            [JsonProperty("scope")] public string Scope { get; set; }
        }

        private readonly IConnectionMultiplexer _muxer;
        private readonly HttpClient _client;
        private readonly ConfigOptions _config;

        public Token(IConnectionMultiplexer muxer, HttpClient client, IOptions<ConfigOptions> options)
        {
            _muxer = muxer;
            _client = client;
            _config = options.Value;
        }

        [FunctionName("token")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "oauth/token")]
            HttpRequest req, ILogger log)
        {
            var deviceCode = req.Form["device_code"].SingleOrDefault();
            var grantType = req.Form["grant_type"].SingleOrDefault();
            var clientId = req.Form["client_id"].SingleOrDefault();

            if (deviceCode == null || grantType == null)
                return new BadRequestObjectResult("device_code, client_id, grant_type are mandatory");

            /*
            if (!grantType.Equals("urn:ietf:params:oauth:grant-type:device_code", StringComparison.OrdinalIgnoreCase) || !grantType.Equals("refresh_token", StringComparison.OrdinalIgnoreCase) )
                return new BadRequestObjectResult("gran_type must be \"urn:ietf:params:oauth:grant-type:device_code\"");
*/
            if (grantType.Equals("refresh_token", StringComparison.OrdinalIgnoreCase))
            {
                
                var scope = req.Form["scope"].SingleOrDefault() ?? "openid";
                var refreshToken = req.Form["refresh_token"];
                
                var b2CTokenResponse = await _client.PostAsync(Helpers.GetTokenEndpoint(_config),
                    new StringContent(
                            $"grant_type=refresh_token&refresh_token={refreshToken}&client_id={_config.AppId}&client_secret={_config.AppSecret}&scope={scope}&")
                        {Headers = {ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")}});
                
                var jwt = await b2CTokenResponse.Content.ReadAsAsync<JObject>();
                
                var accessTokenResponse = new TokenResponse
                {
                    AccessToken = jwt.Value<string>("access_token"),
                    RefreshToken = jwt.Value<string>("refresh_token"),
                    ExpiresIn = jwt.Value<int>("expires_in"),
                    TokenType = jwt.Value<string>("token_type"),
                    Scope =  jwt.Value<string>("token_type")
                };
                
                return new OkObjectResult(accessTokenResponse);
            }
            
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
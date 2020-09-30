using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace Ltwlf.Azure.B2C
{
    public class AuthorizationCallback
    {
        private readonly IConnectionMultiplexer _muxer;
        private readonly HttpClient _client;
        private readonly IConfiguration _config;

        public AuthorizationCallback(IConnectionMultiplexer muxer, HttpClient client, IConfiguration config)
        {
            _muxer = muxer;
            _client = client;
            _config = config;
        }

        [FunctionName("authorization_callback")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string code = req.Query["code"];
            string userCode = req.Query["state"];

            var authState = await Helpers.GetValueByKeyPattern<AuthorizationState>(_muxer, $"*:{userCode}");
            if (authState == null)
            {
                throw new NullReferenceException("Device authentication request expired");
            }

            var tenant = _config.GetValue<string>("B2CTenant");
            var signInFlow = _config.GetValue<string>("SignInFlow");
            var appId = _config.GetValue<string>("AppId");
            var appSecret = _config.GetValue<string>("AppSecret");
            var redirectUri = HttpUtility.UrlEncode(_config.GetValue<string>("RedirectUri"));
            var scope = authState.Scope ?? "openid";

            var tokenResponse = await _client.PostAsync($"{tenant}/{signInFlow}/oauth2/v2.0/token",
                new StringContent(
                        $"grant_type=authorization_code&client_id={appId}&client_secret={appSecret}&scope={scope}&code={code}")
                    {Headers = {ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")}});


            var jwt = await tokenResponse.Content.ReadAsAsync<JObject>();

            authState.AccessToken = jwt.Value<string>("access_token");
            authState.RefreshToken = jwt.Value<string>("refresh_token");
            authState.ExpiresIn = jwt.Value<int>("expires_in");
            authState.TokenType = jwt.Value<string>("token_type");

            _muxer.GetDatabase().StringSet($"{authState.DeviceCode}:{authState.UserCode}",
                JsonConvert.SerializeObject(authState));

            return new OkResult();
        }
    }
}
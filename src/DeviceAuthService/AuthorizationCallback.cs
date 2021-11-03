using System;
using System.IO;
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
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace Ltwlf.Azure.B2C
{
    public class AuthorizationCallback
    {
        private readonly IConnectionMultiplexer _muxer;
        private readonly HttpClient _client;
        private readonly PageFactory _pageFactory;
        private readonly ConfigOptions _config;

        public AuthorizationCallback(IConnectionMultiplexer muxer, HttpClient client, IOptions<ConfigOptions> options, PageFactory pageFactory)
        {
            _muxer = muxer;
            _client = client;
            _pageFactory = pageFactory;
            _config = options.Value;
        }

        [FunctionName("authorization_callback")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "authorization_callback")]
            HttpRequest req, ILogger log, ExecutionContext context)
        {
            log.LogInformation("authorization_callback function processed a request.");

            if(req.Query.ContainsKey("error"))
            {
                return _pageFactory.GetPageResult(PageFactory.PageType.Error);
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            String code = HttpUtility.ParseQueryString(requestBody).Get("code");
            String userCode = HttpUtility.ParseQueryString(requestBody).Get("state");
                        
            var authState = await Helpers.GetValueByKeyPattern<AuthorizationState>(_muxer, $"*:{userCode}");
            if (authState == null)
            {
                log.LogWarning("Device authentication request expired");
                return _pageFactory.GetPageResult(PageFactory.PageType.Error);
            }

            var scope = authState.Scope ?? "openid";

            var tokenResponse = await _client.PostAsync(Helpers.GetTokenEndpoint(_config),
                new StringContent(
                        $"grant_type=authorization_code&client_id={_config.AppId}&client_secret={_config.AppSecret}&scope={scope}&code={code}")
                    {Headers = {ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")}});

            if (tokenResponse.IsSuccessStatusCode == false)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                return new ContentResult() {Content = errorContent};
            }

            var jwt = await tokenResponse.Content.ReadAsAsync<JObject>();

            authState.AccessToken = jwt.Value<string>("access_token");
            authState.RefreshToken = jwt.Value<string>("refresh_token");
            authState.ExpiresIn = jwt.Value<int>("expires_in");
            authState.TokenType = jwt.Value<string>("token_type");

            _muxer.GetDatabase().StringSet($"{authState.DeviceCode}:{authState.UserCode}",
                JsonConvert.SerializeObject(authState), TimeSpan.FromSeconds(_config.ExpiresIn));

            return _pageFactory.GetPageResult(PageFactory.PageType.Success);
        }
    }
}

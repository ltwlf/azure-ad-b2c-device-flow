using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Ltwlf.Azure.B2C
{
    public class SignInRedirect
    {
        private readonly IConnectionMultiplexer _muxer;
        private readonly ConfigOptions _config;

        public SignInRedirect(IConnectionMultiplexer muxer, IOptions<ConfigOptions> options)
        {
            _muxer = muxer;
            _config = options.Value;
        }

        [FunctionName("user_code")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "/")]
            HttpRequest req, ILogger log, ExecutionContext context)
        {
            if (req.Method == "GET")
            {
                var filePath = Path.Combine(context.FunctionDirectory, "../www/userCode.html");

                return new FileStreamResult(File.OpenRead(filePath), "text/html; charset=UTF-8");
            }

            var userCode = req.Form["user_code"];

            var authState = await Helpers.GetValueByKeyPattern<AuthorizationState>(_muxer, $"*:{userCode}");

            var tenant = _config.Tenant;
            var signInFlow = _config.SignInPolicy;
            var appId = _config.AppId;
            var redirectUri = HttpUtility.UrlEncode(_config.RedirectUri);
            var scope = authState.Scope ?? "openid";

            return new RedirectResult(
                $"https://{_config.Tenant}.b2clogin.com/{_config.Tenant}.onmicrosoft.com/oauth2/v2.0/authorize?p={signInFlow}&client_Id={appId}&redirect_uri={redirectUri}&scope={scope}&state={authState.UserCode}&nonce=defaultNonce&response_type=code&prompt=login");
        }
    }
}
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
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Ltwlf.Azure.B2C
{
    public class SignInRedirect
    {
        private readonly IConnectionMultiplexer _muxer;
        private readonly IConfiguration _config;

        public SignInRedirect(IConnectionMultiplexer muxer, IConfiguration config)
        {
            _muxer = muxer;
            _config = config;
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

            var tenant = _config.GetValue<string>("B2CTenant");
            var signInFlow = _config.GetValue<string>("SignInFlow");
            var appId = _config.GetValue<string>("AppId");
            var redirectUri = HttpUtility.UrlEncode(_config.GetValue<string>("RedirectUri"));
            var scope = authState.Scope ?? "openid";

            return new RedirectResult(
                $"{tenant}/oauth2/v2.0/authorize?p={signInFlow}&client_Id={appId}&redirect_uri={redirectUri}&scope={scope}&state={authState.UserCode}&nonce=defaultNonce&response_type=code&prompt=login");
            
        }
    }
}
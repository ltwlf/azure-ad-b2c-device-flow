using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Ltwlf.Azure.B2C
{
    public class SignInRedirect
    {
        private readonly IConnectionMultiplexer _muxer;

        public SignInRedirect(IConnectionMultiplexer muxer)
        {
            _muxer = muxer;
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


            if (authState != null)
            {
                return new RedirectResult(
                    "https://holospacesb2c.b2clogin.com/holospacesb2c.onmicrosoft.com/oauth2/v2.0/authorize?p=B2C_1A_signup_signin&client_id=5e5b80e1-a7c3-45f5-b238-4f1f03896234&nonce=defaultNonce&redirect_uri=http%3A%2F%2Flocalhost%3A7071%2Fauthorization_callback&scope=openid&response_type=code&prompt=login&state=" +
                    authState.UserCode);
            }

            return new UnauthorizedResult();
        }
    }
}
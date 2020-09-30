using System.IO;
using System.Threading.Tasks;
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


            if (authState != null)
            {
                return new RedirectResult(
                    $"{_config.GetValue<string>("SignInFlow")}&state={authState.UserCode}");
            }

            return new UnauthorizedResult();
        }
    }
}
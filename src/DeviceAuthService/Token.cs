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
            var deviceCode = req.Form["device_code"];

            var pattern = $"{deviceCode}:*";

            var key = await Helpers.GetKeyByPattern(_muxer, pattern);
            if (key == null)
            {
                return new UnauthorizedResult();
            }

            var value = _muxer.GetDatabase().StringGet(key);
            var authStateJson = value.ToString();

            var authState = JsonConvert.DeserializeObject<AuthorizationState>(authStateJson);

            if (authState.Token == null)
            {
                var response = new AcceptedResult();
                req.HttpContext.Response.Headers.Add("retry-after",
                    "20"); // To inform the client how long to wait in seconds before checking the status

                return response;
            }

            return new ContentResult
            {
                Content = authState.Token
            };
        }
    }
}
using System;
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
    public class AuthorizationCallback
    {
        private readonly IConnectionMultiplexer _muxer;

        public AuthorizationCallback(IConnectionMultiplexer muxer)
        {
            _muxer = muxer;
        }

        [FunctionName("authorization_callback")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string token = req.Query["code"];
            string userCode = req.Query["state"];

            var authState = await Helpers.GetValueByKeyPattern<AuthorizationState>(_muxer, $"*:{userCode}");
            if (authState == null)
            {
                throw new NullReferenceException("Device authentication request expired");
            }
            
            authState.Token = token;

            _muxer.GetDatabase().StringSet($"{authState.DeviceCode}:{authState.UserCode}", JsonConvert.SerializeObject(authState));

            return new OkResult();
        }
    }
}
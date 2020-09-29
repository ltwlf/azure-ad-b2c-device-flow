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
    public class DeviceAuthorization
    {
        class AuthorizationResponse
        {
            [JsonProperty("device_code")] public string DeviceCode { get; set; }
            [JsonProperty("user_code")] public string UserCode { get; set; }
            [JsonProperty("verification_uri")] public string VerificationUri { get; set; }
            [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
        }

        private readonly IConnectionMultiplexer _muxer;

        public DeviceAuthorization(IConnectionMultiplexer muxer)
        {
            _muxer = muxer;
        }

        [FunctionName("device_authorization")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("DeviceAuthorization function is processing a request.");

            if (req.ContentLength == null || !req.ContentType.Equals("application/x-www-form-urlencoded",
                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("Request content type must be application/x-www-form-urlencoded");
            }

            if (!req.Form.TryGetValue("clientId", out var clientId))
            {
                throw new ArgumentException("ClientId is missing!");
            }

            var authState = new AuthorizationState()
            {
                DeviceCode = Guid.NewGuid().ToString(),
                ClientId = clientId,
                UserCode = new Random().Next(0, 999999).ToString("D6"),
                ExpiresIn = 300,
                VerificationUri = "https://holospaces.com"
            };
            
            var response = new AuthorizationResponse()
            {
                DeviceCode = authState.DeviceCode,
                UserCode = authState.UserCode,
                ExpiresIn = authState.ExpiresIn,
                VerificationUri = authState.VerificationUri
            };

            _muxer.GetDatabase().StringSet($"{authState.DeviceCode}:{authState.UserCode}",
                JsonConvert.SerializeObject(authState), new TimeSpan(0, 0, authState.ExpiresIn));
            
            return new OkObjectResult(JsonConvert.SerializeObject(response));
        }
    }
}
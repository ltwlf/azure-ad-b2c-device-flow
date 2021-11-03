using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Ltwlf.Azure.B2C
{
    public class SignInRedirect
    {
        private readonly IConnectionMultiplexer _muxer;
        private readonly PageFactory _pageFactory;
        private readonly ConfigOptions _config;

        public SignInRedirect(IConnectionMultiplexer muxer, IOptions<ConfigOptions> options, PageFactory pageFactory)
        {
            _muxer = muxer;
            _pageFactory = pageFactory;
            _config = options.Value;
        }

        [FunctionName("user_code_get")]
        public IActionResult Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/")]
            HttpRequest req, ILogger log, ExecutionContext context)
        {
            return _pageFactory.GetPageResult(PageFactory.PageType.UserCode);
        }

        [FunctionName("user_code_post")]
        public async Task<IActionResult> Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "/")]
            HttpRequest req, ILogger log, ExecutionContext context)
        {
            var useAjax = req.Headers.TryGetValue("x-use-ajax", out var useAjaxHeader) &&
                          String.Equals(useAjaxHeader, "true", StringComparison.OrdinalIgnoreCase);

            try
            {
                var userCode = req.Form["user_code"];

                var authState = await Helpers.GetValueByKeyPattern<AuthorizationState>(_muxer, $"*:{userCode}");

                var tenant = _config.Tenant;
                var signInFlow = _config.SignInPolicy;
                var appId = _config.AppId;
                var redirectUri = HttpUtility.UrlEncode(_config.RedirectUri);
                var scope = authState.Scope ?? "openid";

                var url =
                    $"https://{_config.Tenant}.b2clogin.com/{_config.Tenant}.onmicrosoft.com/oauth2/v2.0/authorize?p={signInFlow}&client_Id={appId}&redirect_uri={redirectUri}&scope={scope}&state={authState.UserCode}&nonce=defaultNonce&response_type=code&prompt=login&response_mode=form_post";

                return useAjax
                    ? (IActionResult) new ContentResult {Content = url, ContentType = "text/plain"}
                    : new RedirectResult(url);
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);

                if (useAjax)
                {
                    return new BadRequestResult();
                }

                var filePath = _config.ErrorPage ?? Path.Combine(context.FunctionDirectory, "../www/error.html");
                if (filePath.StartsWith("http"))
                {
                    var client = new WebClient();
                    {
                        return new FileContentResult(client.DownloadData(new Uri(filePath)),
                            "text/html; charset=UTF-8");
                    }
                }

                return new FileStreamResult(File.OpenRead(filePath), "text/html; charset=UTF-8");
            }
        }
    }
}

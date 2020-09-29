using Ltwlf.Azure.B2C;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

[assembly: WebJobsStartup(typeof(Startup))]

namespace Ltwlf.Azure.B2C
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            ConfigureServices(builder.Services);
        }    
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<HttpOptions>(options => options.RoutePrefix = string.Empty);
            var redis = ConnectionMultiplexer.Connect("localhost");
            services.AddSingleton<IConnectionMultiplexer>(redis);
        }
        
    }
}
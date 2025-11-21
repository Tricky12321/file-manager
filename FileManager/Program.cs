using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
namespace FileManager
{
    public static class Program
    {
        
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development || Environment.GetEnvironmentVariable("USE_DEV_SITE") == "true")
            {
                return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().UseKestrel(options => { options.Limits.MaxRequestBodySize = 524288000; });
            }

            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(options => { options.Limits.MaxRequestBodySize = 524288000; });
        }
    }
}

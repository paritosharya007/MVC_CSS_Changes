using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Goalvisor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.Listen(IPAddress.Loopback, 5100);
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ScheduledProcessing.Worker
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
                    webBuilder.UseHealthEndpoints();
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel(options => options.AllowSynchronousIO = true);
                });
    }
}

using aTES.Common.Kafka;
using aTES.Events.SchemaRegistry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace aTES.DayCompletion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var kafkaBrokers = hostContext.Configuration.GetSection("Kafka:Brokers").Get<string[]>();
                    var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();
                    services.AddSingleton<IProducer>(s => new CommonProducer(logger, kafkaBrokers, FailoverPolicy.WithRetry(3)));
                    services.AddPopugEventSchemas(hostContext.Configuration);
                    services.AddHostedService<Worker>();
                });
    }
}

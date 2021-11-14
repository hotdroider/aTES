using aTES.Common.Kafka;
using aTES.Events.SchemaRegistry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                    services.AddSingleton<IProducer>(s => new CommonProducer(kafkaBrokers));
                    services.AddPopugEventSchemas(hostContext.Configuration);
                    services.AddHostedService<Worker>();
                });
    }
}

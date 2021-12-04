using aTES.Common.Kafka;
using aTES.Events.SchemaRegistry;
using aTES.PaymentProcessor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace aTES.PaymentProcessor
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
                    var connectionString = hostContext.Configuration.GetConnectionString("PaymentStoreConnection");
                    services.AddDbContext<PaymentDbContext>(config => config.UseSqlServer(connectionString));

                    services.Configure<MailConfig>(hostContext.Configuration.GetSection("Mail"));
                    var kafkaBrokers = hostContext.Configuration.GetSection("Kafka:Brokers").Get<string[]>();
                    var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();
                    services.AddSingleton<IProducer>(s => new CommonProducer(logger, kafkaBrokers, FailoverPolicy.WithRetry(3)));
                    services.AddSingleton<IConsumerFactory>(s => new ConsumerFactory(kafkaBrokers, logger));
                    services.AddPopugEventSchemas(hostContext.Configuration);
                    services.AddHostedService<PaymentService>();
                    services.AddHostedService<AccountsUpdater>();
                });
    }
}

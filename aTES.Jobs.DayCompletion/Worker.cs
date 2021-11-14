using aTES.Common;
using aTES.Common.Kafka;
using aTES.Events.SchemaRegistry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace aTES.DayCompletion
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly IProducer _producer;
        private readonly SchemaRegistry _schemaRegistry;

        private const string PRODUCER = "aTES.Jobs.DayCompletion";

        public Worker(ILogger<Worker> logger,
            IProducer producer,
            SchemaRegistry schemaRegistry)
        {
            _logger = logger;
            _producer = producer;
            _schemaRegistry = schemaRegistry;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var evnt = new Event()
            {
                Name = "Day.Completed",
                Producer = PRODUCER,
                Version = 1,
                Data = new
                {
                    DateCompleted = DateTime.Today,
                }
            };

            _schemaRegistry.ThrowIfValidationFails(evnt, evnt.Name, evnt.Version);

            await _producer.ProduceAsync(evnt, "Date-completions");

            _logger.LogInformation($"Date {DateTime.Today.ToShortDateString()} completion event produced");
        }
    }
}

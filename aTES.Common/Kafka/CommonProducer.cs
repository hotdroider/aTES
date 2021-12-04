using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace aTES.Common.Kafka
{
    public interface IProducer
    {
        Task ProduceAsync(object message, string topic);
    }

    public class CommonProducer : IProducer, IDisposable
    {
        private IProducer<Null, string> _producer;
        private readonly ILogger _logger;
        private FailoverPolicy _failPolicy;

        public CommonProducer(ILogger logger, string[] servers, FailoverPolicy failPolicy = null)
        {
            _failPolicy = failPolicy ?? FailoverPolicy.Default;
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = string.Join(",", servers),
                ClientId = Dns.GetHostName(),
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
        }


        public Task ProduceAsync(object message, string topic)
        {
            var msg = JsonSerializer.Serialize(message);

            return Policy
                .Handle<ProduceException<Null, string>>()
                .WaitAndRetryAsync(_failPolicy.RetryCount, 
                    retryAttempt => _failPolicy.RetryDelayCalc(retryAttempt),
                    (exception, timeSpan, context) => {
                        _logger.LogWarning($"Cannot send message, retrying");
                    })
                .ExecuteAsync(() => _producer.ProduceAsync(topic, new Message<Null, string>() { Value = msg }));
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }

}

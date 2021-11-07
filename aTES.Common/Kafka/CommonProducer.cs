using Confluent.Kafka;
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

        public CommonProducer(string[] servers)
        {
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

            return _producer.ProduceAsync(topic, new Message<Null, string>() { Value = msg });
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }

}

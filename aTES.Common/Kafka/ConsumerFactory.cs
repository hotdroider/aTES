using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace aTES.Common.Kafka
{
    public interface IConsumerFactory
    {
        /// <summary>
        /// Create builder for consumer
        /// </summary>
        ConsumerBuilder<TMessageData> DefineConsumer<TMessageData>(string consumerGroup, string topic) 
            where TMessageData : class;
    }

    public class ConsumerFactory : IConsumerFactory
    {
        private string[] _servers;
        private ILogger _logger;

        public ConsumerFactory(string[] servers, ILogger logger)
        {
            _servers = servers;
            _logger = logger;
        }

        public ConsumerBuilder<TMessageData> DefineConsumer<TMessageData>(string consumerGroup, string topic) where TMessageData : class
            => new ConsumerBuilder<TMessageData>(_logger, _servers, consumerGroup, topic);
    }

    public class ConsumerBuilder<TMessageData>
        where TMessageData : class
    {
        private FailoverPolicy _failoverPolicy;
        private string _consumerGroup;
        private string _topic;
        private ILogger _logger;
        private string[] _servers;
        private Func<Event<TMessageData>, Task> _processor;

        public ConsumerBuilder(ILogger logger, string[] servers, string consumerGroup, string topic)
        {
            _consumerGroup = consumerGroup;
            _topic = topic;
            _logger = logger;
            _servers = servers;
        }

        public IConsumer Build()
        {
            return new CommonConsumer<TMessageData>(_logger, 
                _servers, 
                _consumerGroup, 
                _topic, 
                _processor, 
                _failoverPolicy);
        }

        public ConsumerBuilder<TMessageData> SetFailoverPolicy(FailoverPolicy policy)
        {
            _failoverPolicy = policy;
            return this;
        }

        public ConsumerBuilder<TMessageData> SetProcessor(Func<Event<TMessageData>, Task> processor)
        {
            _processor = processor;
            return this;
        }
    }
}

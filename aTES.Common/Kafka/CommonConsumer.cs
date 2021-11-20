using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace aTES.Common.Kafka
{
    public interface IConsumer
    {
        public Task ProcessAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Generic consumer processor for event with some payload type
    /// </summary>
    public class CommonConsumer<TMessageData> : IConsumer, IDisposable
        where TMessageData: class
    {
        private IConsumer<Ignore, string> _consumer;
        private readonly ILogger _logger;
        private FailoverPolicy _failPolicy;
        private IProducer _failProducer;
        private string _topic;

        private Func<Event<TMessageData>, Task> _messageProcessor;

        public CommonConsumer(ILogger logger, 
            string[] servers, 
            string consumerGroup, 
            string topic,
            Func<Event<TMessageData>, Task> messageProcessor,
            FailoverPolicy failoverPolicy = null)
        {
            _logger = logger;
            _failPolicy = failoverPolicy ?? FailoverPolicy.Default;
            _topic = topic;
            _messageProcessor = messageProcessor;

            var config = new ConsumerConfig
            {
                BootstrapServers = string.Join(",", servers),
                GroupId = consumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            _consumer.Subscribe(topic);
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            if (_messageProcessor == null)
                throw new ApplicationException("Processor not set");

            while(!cancellationToken.IsCancellationRequested)
            {
                string eventBody = null;
                try
                {
                    //receive next message
                    var msg = await Policy<ConsumeResult<Ignore, string>>
                        .Handle<ConsumeException>()
                        .WaitAndRetryAsync(_failPolicy.RetryCount,
                            retryAttempt => _failPolicy.RetryDelayCalc(retryAttempt),
                            (exception, timeSpan, context) =>
                            {
                                _logger.LogWarning($"Cannot receive message, retrying");
                            })
                        .ExecuteAsync(async () => await Task.Run(() =>_consumer.Consume(cancellationToken)));

                    //save body and deserialize to Event object
                    eventBody = msg.Message.Value;
                    var eventObj = JsonSerializer.Deserialize<Event<TMessageData>>(eventBody);

                    //delegate processor
                    await _messageProcessor(eventObj);

                    //we ok, commit
                    _consumer.Commit(msg);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    if (_failPolicy.ReproduceToDLQ)
                    {
                        _failProducer?.ProduceAsync(eventBody, _failPolicy.DeadLetterQueueNameBuilder(_topic));
                    }
                    throw;
                }
            }
        }

        public void Dispose()
        {
            _consumer?.Dispose();
        }
    }
}

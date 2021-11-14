using Confluent.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace aTES.Common.Kafka
{
    public interface IConsumer
    {
        public Task<ICommitablaMessage<Ignore, string>> ConsumeAsync(CancellationToken cancellationToken);
    }

    public interface ICommitablaMessage<TKey, TValue>
    {
        public TKey Key { get; }

        public TValue Value { get; }

        public void Commit();
    }

    public class CommonConsumer : IConsumer, IDisposable
    {
        private IConsumer<Ignore, string> _consumer;

        public CommonConsumer(string[] servers, string consumerGroup, string topic)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = string.Join(",", servers),
                GroupId = consumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            _consumer.Subscribe(topic);
        }

        public async Task<ICommitablaMessage<Ignore, string>> ConsumeAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                while(true)
                try
                {
                    var result = _consumer.Consume(cancellationToken);
                    return new ReceivedMessage<Ignore, string>(result, _consumer);
                }
                catch(ConsumeException)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            });
        }

        public string Consume(CancellationToken cancellationToken = default)
        {
            var result = _consumer.Consume(cancellationToken);
            //_consumer.Commit(result);

            return result.Message.Value;
        }

        public void Dispose()
        {
            _consumer?.Dispose();
        }

        private class ReceivedMessage<TKey, TValue> : ICommitablaMessage<TKey, TValue>
        {
            private ConsumeResult<TKey, TValue> _result;
            private IConsumer<TKey, TValue> _consumer;

            public ReceivedMessage(ConsumeResult<TKey, TValue> result, IConsumer<TKey, TValue> consumer)
            {
                _result = result;
                _consumer = consumer;
            }

            public TKey Key => _result.Message.Key;

            public TValue Value => _result.Message.Value;

            public void Commit()
            {
                _consumer.Commit(_result);
            }
        }
    }
}

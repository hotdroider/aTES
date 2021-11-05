namespace aTES.Common.Kafka
{
    public interface IConsumerFactory
    {
        IConsumer CreateConsumer(string consumerGroup, string topic);
    }

    public class ConsumerFactory : IConsumerFactory
    {
        private string[] _servers;

        public ConsumerFactory(string[] servers)
        {
            _servers = servers;
        }

        public IConsumer CreateConsumer(string consumerGroup, string topic) => new CommonConsumer(_servers, consumerGroup, topic);
    }
}

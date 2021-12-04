using System;

namespace aTES.Common.Kafka
{
    /// <summary>
    /// What to do on MB failures
    /// </summary>
    public record FailoverPolicy
    {
        /// <summary>
        /// Times to try, only once by default
        /// </summary>
        public int RetryCount { get; set; } = 1;

        /// <summary>
        /// Delay calc policy
        /// </summary>
        public Func<int, TimeSpan> RetryDelayCalc { get; set; } = (retryAttempt) => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));

        /// <summary>
        /// Fire to DLQ on failure
        /// </summary>
        public bool ReproduceToDLQ { get; set; } = false;

        /// <summary>
        /// DLQ topic name builder
        /// </summary>
        public Func<string, string> DeadLetterQueueNameBuilder { get; set; } = (originalName) => $"{originalName}_DeadLetterQueue";

        /// <summary>
        /// Default params
        /// </summary>
        public static readonly FailoverPolicy Default = new FailoverPolicy();

        /// <summary>
        /// Send to dlq on fail
        /// </summary>
        public static readonly FailoverPolicy ToDlq = new FailoverPolicy() {  ReproduceToDLQ = true };

        /// <summary>
        /// Retry some
        /// </summary>
        public static FailoverPolicy WithRetry(int retryTimes) => new FailoverPolicy() { RetryCount = retryTimes };
    }
}

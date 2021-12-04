using aTES.Analytics.Data;
using aTES.Common;
using aTES.Common.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace aTES.Analytics.Services
{
    /// <summary>
    /// Service to listen for billing transactions
    /// </summary>
    public class BillingHistoryUpdater : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IConsumer _accountConsumer;

        public BillingHistoryUpdater(IServiceScopeFactory scopeFactory,
            IConsumerFactory consumerFactory)
        {
            _scopeFactory = scopeFactory;
            _accountConsumer = consumerFactory
                .DefineConsumer<TransactionData>("aTES.Analytics.BillingHistoryUpdater", "BillingTransactions")
                .SetFailoverPolicy(FailoverPolicy.ToDlq)
                .SetProcessor(ProcessTransactionMessage)
                .Build();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => _accountConsumer.ProcessAsync(stoppingToken);

        private async Task ProcessTransactionMessage(Event<TransactionData> accMsg)
        {
            var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();

            var hist = new TransactionHistory()
            {
                AccountPublicId = accMsg.Data.AccountPublicKey,
                Date = accMsg.Data.Date,
                Reason = accMsg.Data.Reason
            };

            switch (accMsg.Name, accMsg.Version)
            {
                case ("Accounts.Debited", 1):
                    hist.Debit = accMsg.Data.Amount;
                    hist.Type = TransactionType.Debit;
                    break;
                //not really need to save dayly payments but just in case
                case ("Accounts.Paid", 1):
                    hist.Credit = accMsg.Data.Amount;
                    hist.Type = TransactionType.Payment;
                    break;
                case ("Accounts.Credited", 1):
                    hist.Credit = accMsg.Data.Amount;
                    hist.Type = TransactionType.Credit;
                    break;
                default:
                    throw new Exception($"Unsupported message {accMsg.Name}{accMsg.Version}");
            };

            db.Transactions.Add(hist);

            await db.SaveChangesAsync();
        }

        private class TransactionData
        {
            public string AccountPublicKey { get; set; }

            public DateTime Date { get; set; }

            public decimal Amount { get; set; }

            public string Reason { get; set; }
        }
    }
}

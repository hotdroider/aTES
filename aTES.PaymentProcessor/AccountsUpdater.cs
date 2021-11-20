using aTES.Common;
using aTES.Common.Kafka;
using aTES.PaymentProcessor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace aTES.PaymentProcessor
{
    /// <summary>
    /// Service to listen for Account CUD events
    /// </summary>
    public class AccountsUpdater : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IConsumer _accountConsumer;

        public AccountsUpdater(IServiceScopeFactory scopeFactory,
            IConsumerFactory consumerFactory)
        {
            _scopeFactory = scopeFactory;
            _accountConsumer = consumerFactory
                .DefineConsumer<AccountData>("aTES.PaymentProcessor.AccountUpdater", "Accounts-stream")
                .SetFailoverPolicy(FailoverPolicy.ToDlq)
                .SetProcessor(ProcessAccountMessage)
                .Build();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => _accountConsumer.ProcessAsync(stoppingToken);

        private async Task ProcessAccountMessage(Event<AccountData> accMsg)
        {
            var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            switch (accMsg.Name, accMsg.Version)
            {
                case ("Accounts.Created", 1):
                case ("Accounts.Updated", 1):
                    await CreateOrUpdateAccount(accMsg.Data, db);
                    break;
                case ("Accounts.Deleted", 1):
                    return;
                default:
                    throw new Exception($"Unsupported message {accMsg.Name}{accMsg.Version}");
            };
        }

        private async Task CreateOrUpdateAccount(AccountData accEvent, PaymentDbContext db)
        {
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.PublicKey == accEvent.PublicKey);
            if (account == null)
            {
                account = new Account()
                {
                    PublicKey = accEvent.PublicKey,
                };
                await db.Accounts.AddAsync(account);
            }

            account.Email = accEvent.Email;

            await db.SaveChangesAsync();
        }

        private class AccountData
        {
            public string Name { get; set; }

            public string PublicKey { get; set; }

            public string Email { get; set; }

            public string Role { get; set; }
        }
    }
}

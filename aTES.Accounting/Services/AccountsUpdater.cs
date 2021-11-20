using aTES.Accounting.Data;
using aTES.Common;
using aTES.Common.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace aTES.Accounting.Services
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
                .DefineConsumer<AccountData>("aTES.Accounting.AccountUpdater", "Accounts-stream")
                .SetFailoverPolicy(FailoverPolicy.ToDlq)
                .SetProcessor(ProcessAccountMessage)
                .Build();
        }

        private async Task ProcessAccountMessage(Event<AccountData> accMsg)
        {
            var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
            switch (accMsg.Name, accMsg.Version)
            {
                case ("Accounts.Created", 1):
                case ("Accounts.Updated", 1):
                    await CreateOrUpdateAccount(accMsg.Data, db);
                    break;
                case ("Accounts.Deleted", 1):
                    await DeleteAccount(accMsg.Data, db);
                    break;
                default:
                    throw new Exception($"Unsupported message {accMsg.Name}{accMsg.Version}");
            };
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _accountConsumer.ProcessAsync(stoppingToken);
        }

        private async Task CreateOrUpdateAccount(AccountData accEvent, AccountingDbContext db)
        {
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.PublicKey == accEvent.PublicKey);
            if (account == null)
            {
                account = new Account()
                {
                    PublicKey = accEvent.PublicKey,
                    IsDeleted = false
                };
                await db.Accounts.AddAsync(account);
            }

            account.Name = accEvent.Name;
            account.Email = accEvent.Email;
            account.Role = accEvent.Role;

            await db.SaveChangesAsync();
        }

        private async Task DeleteAccount(AccountData accEvent, AccountingDbContext db)
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

            account.IsDeleted = true;

            await db.SaveChangesAsync();
        }


        //need some class to deserialize to

        private class AccountData
        {
            public string Name { get; set; }

            public string PublicKey { get; set; }

            public string Email { get; set; }

            public string Role { get; set; }
        }
    }
}

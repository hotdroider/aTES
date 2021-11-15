using aTES.Common.Kafka;
using aTES.Accounting.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using aTES.Common;

namespace aTES.Accounting.Services
{
    /// <summary>
    /// Service to listen for Account CUD events
    /// </summary>
    public class AccountsUpdater : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IConsumer _accountConsumer;
        private readonly IProducer _producer;

        public AccountsUpdater(IServiceScopeFactory scopeFactory, 
            IConsumerFactory consumerFactory,
            IProducer producer)
        {
            _scopeFactory = scopeFactory;
            _producer = producer;
            _accountConsumer = consumerFactory.CreateConsumer("aTES.Accounting.AccountUpdater", "Accounts-stream");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
            while (!stoppingToken.IsCancellationRequested)
            {
                string eventBody = null;
                try
                {
                    var msgRaw = await _accountConsumer.ConsumeAsync(stoppingToken);
                    eventBody = msgRaw.Value;
                    var accMsg = JsonSerializer.Deserialize<Event<AccountData>>(eventBody);

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

                    msgRaw.Commit();
                }
                catch (Exception ex)
                {
                    await TryReproduceToDLQ(eventBody);
                }
            }
        }

        private async Task TryReproduceToDLQ(string messageBody)
        {
            try
            {
                await _producer.ProduceAsync(messageBody, "Accounts-Dead-Letter-Queue");
            }
            catch(Exception ex)
            {
                //log, panic, save to db...
            }
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

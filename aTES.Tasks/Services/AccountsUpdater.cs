using aTES.Common.Kafka;
using aTES.Tasks.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace aTES.Tasks.Services
{
    /// <summary>
    /// Service to listen for Account CUD events
    /// </summary>
    public class AccountsUpdater : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IConsumer _accountConsumer;

        public AccountsUpdater(IServiceScopeFactory scopeFactory, IConsumerFactory consumerFactory)
        {
            _scopeFactory = scopeFactory;

            _accountConsumer = consumerFactory.CreateConsumer("aTES.Tasks.AccountUpdater", "Accounts-stream");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TasksDbContext>();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var msgRaw = await _accountConsumer.ConsumeAsync();
                    var accMsg = JsonSerializer.Deserialize<AccMessage>(msgRaw.Value);

                    await ProcessMessage(accMsg, db);

                    msgRaw.Commit();
                }
                catch (Exception ex)
                {
                    //TODO 
                }
            }
        }

        private async Task ProcessMessage(AccMessage accMsg, TasksDbContext db)
        {
            //message body is full here, just insert or update
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.PublicKey == accMsg.Account.PublicKey);
            if (account == null)
            {
                account = new Account();
                await db.Accounts.AddAsync(account);
            }

            account.Name = accMsg.Account.Name;
            account.Email = accMsg.Account.Email;
            account.PublicKey = accMsg.Account.PublicKey;
            account.Role = accMsg.Account.Role;
            account.IsDeleted = accMsg.Account.IsDeleted == true;

            await db.SaveChangesAsync();
        }
    }

    public class AccMessage
    {
        public class AccBody
        {
            public string Name { get; set; }

            public string PublicKey { get; set; }

            public string Email { get; set; }

            public string Role { get; set; }

            public bool? IsDeleted { get; set; }
        }

        public string Type { get; set; }

        public AccBody Account { get; set; }
    }
}

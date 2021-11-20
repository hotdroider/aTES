using aTES.Accounting.Data;
using aTES.Common;
using aTES.Common.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace aTES.Accounting.Services
{
    /// <summary>
    /// Listener to task state changed and cycle ends
    /// </summary>
    public class BillingProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IConsumer _taskChangeConsumer;
        private readonly IConsumer _cycleEndConsumer;
        private readonly IProducer _producer;

        public BillingProcessor(IServiceScopeFactory scopeFactory,
            IConsumerFactory consumerFactory,
            IProducer producer)
        {
            _scopeFactory = scopeFactory;
            _producer = producer;

            _taskChangeConsumer = consumerFactory
                .DefineConsumer<TaskStateData>("aTES.Accounting.BillingProcessor", "Tasks-state-changes")
                .SetFailoverPolicy(FailoverPolicy.ToDlq)
                .SetProcessor(ProcessTaskAsync)
                .Build();

            _cycleEndConsumer = consumerFactory
                .DefineConsumer<DateCompletedData>("aTES.Accounting.CycleEndProcessor", "Date-completions")
                .SetFailoverPolicy(FailoverPolicy.ToDlq)
                .SetProcessor(ProcessDateCloseAsync)
                .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var taskChanges = _taskChangeConsumer.ProcessAsync(stoppingToken);
            var cycleEnds = _cycleEndConsumer.ProcessAsync(stoppingToken);

            await Task.WhenAll(taskChanges, cycleEnds);
        }

        private async Task ProcessTaskAsync(Event<TaskStateData> taskStateMsg)
        {
            switch (taskStateMsg.Name, taskStateMsg.Version)
            {
                case ("Tasks.Assigned", 1):
                    await ChargeTaskAssignement(taskStateMsg.Data.PublicKey, taskStateMsg.Data.AssigneePublicKey);
                    break;
                case ("Tasks.Completed", 1):
                    await AwardTask(taskStateMsg.Data.PublicKey, taskStateMsg.Data.AssigneePublicKey);
                    break;
                default:
                    throw new Exception($"Unsupported message {taskStateMsg.Name}{taskStateMsg.Version}");
            };
        }

        private async Task ProcessDateCloseAsync(Event<DateCompletedData> dateCloseData)
        {
            switch (dateCloseData.Name, dateCloseData.Version)
            {
                case ("Day.Completed", 1):
                    await ClosePaymentCyclesAsync(dateCloseData.Data.DateCompleted);
                    break;
                default:
                    throw new Exception($"Unsupported message {dateCloseData.Name}{dateCloseData.Version}");
            };
        }

        /// <summary>
        /// Calc and add award tran
        /// </summary>
        private async Task AwardTask(string taskPublicId, string popugPublicId)
        {
            var db = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<AccountingDbContext>();

            var popug = await GetOrAddPopug(db, popugPublicId);
            var task = await GetOrAddTask(db, taskPublicId);

            var cycle = await GetOrAddCycle(db, popug.Id);
            var tran = new Transaction()
            {
                TaskId = task?.Id,
                Type = TransactionType.Credit,
                Amount = new Random(Guid.NewGuid().GetHashCode()).Next(20, 40)
            };
            cycle.Transactions.Add(tran);
            cycle.Amount += tran.Amount;

            await db.SaveChangesAsync();
        }


        /// <summary>
        /// Calc and add charge tran
        /// </summary>
        private async Task ChargeTaskAssignement(string taskPublicId, string popugPublicId)
        {
            var db = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<AccountingDbContext>();

            var popug = await GetOrAddPopug(db, popugPublicId);
            var task = await GetOrAddTask(db, taskPublicId);

            //task fee is calculated only once, then stored and reused
            if (task.AssignFee == null)
                task.AssignFee = new Random(Guid.NewGuid().GetHashCode()).Next(20, 40);

            var cycle = await GetOrAddCycle(db, popug.Id);
            var tran = new Transaction()
            {
                TaskId = task?.Id,
                Type = TransactionType.Credit,
                Amount = task.AssignFee.Value
            };
            cycle.Transactions.Add(tran);
            cycle.Amount -= tran.Amount;

            await db.SaveChangesAsync();
        }

        private async Task<Account> GetOrAddPopug(AccountingDbContext db, string publicKey)
        {
            var popug = await db.Accounts.FirstOrDefaultAsync(a => a.PublicKey == publicKey);
            if (popug == null) //we need popug record, hope this updates by CUD later...
            {
                popug = new Account() { PublicKey = publicKey, IsDeleted = false };
                db.Accounts.Add(popug);
                await db.SaveChangesAsync();
            }

            return popug;
        }

        private async Task<PopugTask> GetOrAddTask(AccountingDbContext db, string publicKey)
        {
            var task = await db.Tasks.FirstOrDefaultAsync(a => a.PublicKey == publicKey);
            if (task == null)  //we need task record, hope this updates by CUD later...
            {
                task = new PopugTask() { PublicKey = publicKey, Name = "Unknown" };
                db.Tasks.Add(task);
                await db.SaveChangesAsync();
            }

            if (task.AssignFee == null)
                task.AssignFee = new Random(Guid.NewGuid().GetHashCode()).Next(10, 20);

            return task;
        }

        private async Task<BillingCycle> GetOrAddCycle(AccountingDbContext db, int popugId)
        {
            var cycle = await db.BillingCycles.FirstOrDefaultAsync(a => a.AccountId == popugId && a.State == BillingCycleState.Open);
            if (cycle == null)  //we need task record, hope this updates by CUD later...
            {
                cycle = new BillingCycle()
                {
                    AccountId = popugId,
                    State = BillingCycleState.Open,
                    Date = DateTime.Today,
                };
                db.BillingCycles.Add(cycle);
                await db.SaveChangesAsync();
            }

            return cycle;
        }

        private async Task ClosePaymentCyclesAsync(DateTime completedDate)
        {
            var db = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<AccountingDbContext>();

            var openedCycles = await db.BillingCycles.Where(c => c.State == BillingCycleState.Open && c.Date == completedDate).ToListAsync();

            foreach (var cycle in openedCycles)
            {
                //calc popugs earnings
                var amount = cycle.Transactions.Sum(t =>
                    t.Type == TransactionType.Debit ? -t.Amount : //assignes
                    t.Type == TransactionType.Credit ? t.Amount : //completion awards
                    t.Type == TransactionType.Init ? -t.Amount : //previous day debt
                    0);

                var tran = new Transaction()
                {
                    Amount = amount,
                    BillingCycleId = cycle.Id,
                    Type = TransactionType.Payment,
                };
                cycle.Transactions.Add(tran);
            }

            await db.SaveChangesAsync();
        }

        private class TaskStateData
        {
            public string PublicKey { get; set; }

            public string AssigneePublicKey { get; set; }
        }

        private class DateCompletedData
        {
            public DateTime DateCompleted { get; set; }
        }
    }
}

using aTES.Accounting.Data;
using aTES.Common;
using aTES.Common.Kafka;
using aTES.Events.SchemaRegistry;
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
        private readonly SchemaRegistry _schemaRegistry;

        public const string PRODUCER = "aTES.Accounting";

        public BillingProcessor(IServiceScopeFactory scopeFactory,
            IConsumerFactory consumerFactory,
            IProducer producer,
            SchemaRegistry schemaRegistry)
        {
            _scopeFactory = scopeFactory;
            _producer = producer;
            _schemaRegistry = schemaRegistry;

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
                Date = DateTime.UtcNow,
                Task = task,
                BillingCycle = cycle,
                Type = TransactionType.Credit,
                Credit = new Random(Guid.NewGuid().GetHashCode()).Next(20, 40)
            };
            cycle.Transactions.Add(tran);
            cycle.Amount += tran.Credit;

            await db.SaveChangesAsync();

            await SendTransactionEventAsync("Accounts.Credited", 1, tran);
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
                Date = DateTime.UtcNow,
                Task = task,
                BillingCycle = cycle,
                Type = TransactionType.Credit,
                Debit = task.AssignFee.Value
            };
            cycle.Transactions.Add(tran);
            cycle.Amount -= tran.Debit;

            await db.SaveChangesAsync();

            await SendTransactionEventAsync("Accounts.Debited", 1, tran);
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
                cycle.State = BillingCycleState.Closed;

                //calc popugs earnings
                var amount = cycle.Transactions.Sum(t => t.Credit - t.Debit);

                if (amount > 0)
                {
                    //pay popugs his day 
                    var tran = new Transaction()
                    {
                        Credit = amount,
                        Date = DateTime.UtcNow,
                        BillingCycle = cycle,
                        BillingCycleId = cycle.Id,
                        Type = TransactionType.Payment,
                    };
                    cycle.Transactions.Add(tran);

                    await db.SaveChangesAsync();

                    await SendTransactionEventAsync("Accounts.Paid", 1, tran);
                }
                else
                {
                    //move debt to next cycle
                    var newCycle = await GetOrAddCycle(db, cycle.AccountId);
                    //mover to next
                    var tran = new Transaction()
                    {
                        Debit = -amount,
                        Date = DateTime.UtcNow.AddDays(1).Date, //init debt for tomorrow
                        BillingCycle = cycle,
                        BillingCycleId = cycle.Id,
                        Type = TransactionType.Init,
                    };
                    newCycle.Transactions.Add(tran);
                    db.BillingCycles.Add(newCycle);

                    await db.SaveChangesAsync();

                    await SendTransactionEventAsync("Accounts.Debited", 1, tran);
                }
            }
        }

        /// <summary>
        /// Send business transaction event
        /// </summary>
        private async Task SendTransactionEventAsync(string eventType, int version, Transaction transaction)
        {
            var evnt = new Event()
            {
                Name = eventType,
                Producer = PRODUCER,
                Version = version
            };

            var task = transaction.Task;
            var db = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<AccountingDbContext>();
            var publicAccId = await db.Accounts.Where(acc => acc.Id == transaction.BillingCycle.AccountId).Select(a => a.PublicKey).FirstOrDefaultAsync();

            evnt.Data = eventType switch
            {
                "Accounts.Credited" => new
                {
                    AccountPublicKey = publicAccId,
                    Amount = transaction.Credit,
                    Date = transaction.Date,
                    Reason = $"Completion of task {task.Id}, {task.JiraId} {task.Name}",
                },
                "Accounts.Debited" => new
                {
                    AccountPublicKey = publicAccId,
                    Amount = transaction.Debit,
                    Date = transaction.Date,
                    Reason = $"Assignation of task {task.Id}, {task.JiraId} {task.Name}",
                },
                "Accounts.Paid" => new
                {
                    AccountPublicKey = publicAccId,
                    Amount = transaction.Credit,
                    Date = transaction.Date,
                    Reason = $"Payment for {transaction.Date.ToShortDateString()} working day",
                },
                _ => throw new ArgumentException($"Unsupported event type {eventType}")
            };

            _schemaRegistry.ThrowIfValidationFails(evnt, eventType, version);

            await _producer.ProduceAsync(evnt, "BillingTransactions");
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

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
    /// Service to listen for Tasks CUD events
    /// </summary>
    public class TaskUpdater : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IConsumer _accountConsumer;
        private readonly IProducer _producer;

        public TaskUpdater(IServiceScopeFactory scopeFactory, 
            IConsumerFactory consumerFactory,
            IProducer producer)
        {
            _scopeFactory = scopeFactory;
            _producer = producer;
            _accountConsumer = consumerFactory.CreateConsumer("aTES.Accounting.TaskUpdater", "Tasks-stream");
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
                    var taskMsg = JsonSerializer.Deserialize<Event<TaskData>>(eventBody);

                    //this is v2 consumer with backward compability to v1
                    switch (taskMsg.Name, taskMsg.Version)
                    {
                        //initial format [obsoleted]
                        case ("Tasks.Created", 1):
                        case ("Tasks.Updated", 1):
                            {
                                //naive
                                taskMsg.Data.JiraId = taskMsg.Data.Name.IndexOf('[') < 0 
                                    ? null 
                                    : taskMsg.Data.Name.Substring(taskMsg.Data.Name.IndexOf('['), taskMsg.Data.Name.IndexOf(']')  + 1);
                                await CreateOrUpdateTasks(taskMsg.Data, db);
                            }
                            break;
                        //jira-id added in v2
                        case ("Tasks.Created", 2):
                        case ("Tasks.Updated", 2):
                            await CreateOrUpdateTasks(taskMsg.Data, db);
                            break;
                        default:
                            throw new Exception($"Unsupported message {taskMsg.Name}{taskMsg.Version}");
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

        private async Task CreateOrUpdateTasks(TaskData taskData, AccountingDbContext db)
        {
            var task = await db.Tasks.FirstOrDefaultAsync(a => a.PublicKey == taskData.PublicKey);
            if (task == null)
            {
                task = new PopugTask()
                {
                    PublicKey = taskData.PublicKey,
                };
                await db.Tasks.AddAsync(task);
            }

            task.Name = taskData.Name;
            task.Description = taskData.Description;
            task.JiraId = taskData.JiraId;

            await db.SaveChangesAsync();
        }

        //need some class to deserialize to

        private class TaskData
        {
            public string Name { get; set; }

            public string PublicKey { get; set; }

            public string Description { get; set; }

            public string JiraId { get; set; }
        }
    }
}

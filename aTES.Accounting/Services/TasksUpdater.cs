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
    /// Service to listen for Tasks CUD events
    /// </summary>
    public class TaskUpdater : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IConsumer _accountConsumer;

        public TaskUpdater(IServiceScopeFactory scopeFactory,
            IConsumerFactory consumerFactory)
        {
            _scopeFactory = scopeFactory;
            _accountConsumer = consumerFactory
                .DefineConsumer<TaskData>("aTES.Accounting.TaskUpdater", "Tasks-stream")
                .SetFailoverPolicy(FailoverPolicy.ToDlq)
                .SetProcessor(ProcessTaskMessageAsync)
                .Build();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => _accountConsumer.ProcessAsync(stoppingToken);

        private async Task ProcessTaskMessageAsync(Event<TaskData> taskMsg)
        {
            var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();

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
                            : taskMsg.Data.Name.Substring(taskMsg.Data.Name.IndexOf('['), taskMsg.Data.Name.IndexOf(']') + 1);
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

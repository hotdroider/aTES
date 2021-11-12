using aTES.Common;
using aTES.Common.Kafka;
using aTES.Events.SchemaRegistry;
using aTES.Tasks.Data;
using aTES.Tasks.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aTES.Tasks.Services
{
    public class TaskService
    {
        private readonly TasksDbContext _tasksDbContext;

        private readonly IProducer _producer;

        private readonly SchemaRegistry _schemaRegistry;

        private const string PRODUCER = "aTES.Tasks";

        public TaskService(TasksDbContext tasksDbContext,
            IProducer producer,
            SchemaRegistry schemaRegistry)
        {
            _tasksDbContext = tasksDbContext;
            _producer = producer;
            _schemaRegistry = schemaRegistry;
        }

        /// <summary>
        /// Create new task, assign on some popug in progress
        /// </summary>
        public async Task CreateAsync(AddTask model)
        {
            if (model.Name.Any(c => c == '[' || c == ']'))
                throw new ArgumentException("Title should not contains Jira ID");

            var newTask = new PopugTask()
            {
                Name = model.Name,
                Description = model.Description,
                PublicKey = Guid.NewGuid().ToString(),
                Status = TaskState.Open,
            };

            //task must always be assigned, even new one
            newTask.AssignedOn = await GetUnluckyDeveloperAsync();

            _tasksDbContext.Tasks.Add(newTask);
            await _tasksDbContext.SaveChangesAsync();

            await SendCUDAsync("Tasks.Created", 2, newTask);
            await SendTaskStateEventAsync("Tasks.Assigned", 1, newTask);
        }

        /// <summary>
        /// Get popugs tasks
        /// </summary>
        public async Task<IList<PopugTask>> GetTasks(string assignee)
        {
            return await _tasksDbContext.Tasks
                .Where(t => t.AssignedOn == assignee)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Reassign all opened tasks on random developers
        /// </summary>
        public async Task ShuffleTasksAsync()
        {
            var openedTasks = await _tasksDbContext.Tasks
                .Where(t => t.Status == TaskState.Open)
                .ToListAsync();

            foreach(var task in openedTasks)
                task.AssignedOn = await GetUnluckyDeveloperAsync();

            await _tasksDbContext.SaveChangesAsync();

            foreach (var task in openedTasks)
                await SendTaskStateEventAsync("Tasks.Assigned", 1, task);
        }

        /// <summary>
        /// Set task completed
        /// </summary>
        public async Task CompleteTask(int taskId)
        {
            var task  = await _tasksDbContext.Tasks.FindAsync(taskId);
            task.Status = TaskState.Completed;

            await _tasksDbContext.SaveChangesAsync();

            await SendTaskStateEventAsync("Tasks.Completed", 1, task);
        }

        /// <summary>
        /// Random developer public key
        /// </summary>
        private async Task<string> GetUnluckyDeveloperAsync()
        {
            var developerIds = await _tasksDbContext.Accounts
                .Where(acc => acc.Role == "Developer" && !acc.IsDeleted)
                .Select(acc => acc.PublicKey)
                .ToListAsync();

            if (developerIds.Count == 0)
                throw new Exception("No developers found");

            var roll = new Random(Guid.NewGuid().GetHashCode());
            var idx = roll.Next(0, developerIds.Count - 1);

            return developerIds[idx];
        }

        /// <summary>
        /// Send business task event
        /// </summary>
        private Task SendTaskStateEventAsync(string eventType, int version, PopugTask task)
        {
            var evnt = new Event()
            {
                Name = eventType,
                Producer = PRODUCER,
                Version = version
            };

            evnt.Data = eventType switch
            {
                "Tasks.Assigned" => new
                {
                    PublicKey = task.PublicKey,
                    AssigneePublicKey = task.AssignedOn,
                },
                "Tasks.Completed" => new
                {
                    PublicKey = task.PublicKey,
                    AssigneePublicKey = task.AssignedOn,
                },
                _ => throw new ArgumentException($"Unsupported event type {eventType}")
            };

            _schemaRegistry.ThrowIfValidationFails(evnt, eventType, version);

            return _producer.ProduceAsync(evnt, "Tasks-state-changes");
        }

        /// <summary>
        /// Build, validate and send CUD event for task
        /// </summary>
        private Task SendCUDAsync(string eventType, int version, PopugTask task)
        {
            var evnt = new Event()
            {
                Name = eventType,
                Producer = PRODUCER,
                Version = version
            };

            evnt.Data = eventType switch
            {
                "Tasks.Created" => new
                {
                    Name = task.Name,
                    PublicKey = task.PublicKey,
                    Description = task.Description,
                },
                "Tasks.Updated" => new
                {
                    Name = task.Name,
                    PublicKey = task.PublicKey,
                    Description = task.Description,
                },
                _ => throw new ArgumentException($"Unsupported event type {eventType}")
            };

            _schemaRegistry.ThrowIfValidationFails(evnt, eventType, version);

            return _producer.ProduceAsync(evnt, "Tasks-stream");
        }
    }
}

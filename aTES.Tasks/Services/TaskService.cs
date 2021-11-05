using aTES.Common.Kafka;
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
        public readonly TasksDbContext _tasksDbContext;

        public readonly IProducer _producer;

        public TaskService(TasksDbContext tasksDbContext,
            IProducer producer)
        {
            _tasksDbContext = tasksDbContext;
            _producer = producer;
        }

        /// <summary>
        /// Create new task, assign on some popug in progress
        /// </summary>
        public async Task CreateAsync(AddTask model)
        {
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

            await SendCUDAsync("Created", newTask, "Tasks-stream");
            await SendBusinessAsync("Assigned", newTask, "Tasks");
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
            {
                await SendCUDAsync("Updated", task, "Tasks-stream");
                await SendBusinessAsync("Assigned", task, "Tasks");
            }
        }

        /// <summary>
        /// Set task completed
        /// </summary>
        public async Task CompleteTask(int taskId)
        {
            var task  = await _tasksDbContext.Tasks.FindAsync(taskId);
            task.Status = TaskState.Completed;

            await _tasksDbContext.SaveChangesAsync();

            await SendCUDAsync("Updated", task, "Tasks-stream");
            await SendBusinessAsync("Completed", task, "Tasks");
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
                return null;

            var roll = new Random(Guid.NewGuid().GetHashCode());
            var idx = roll.Next(0, developerIds.Count - 1);

            return developerIds[idx];
        }

        /// <summary>
        /// Send business task event
        /// </summary>
        private Task SendBusinessAsync(string messageType, PopugTask task, string topic)
        {
            return _producer.ProduceAsync(new
            {
                Type = messageType,
                At = DateTime.UtcNow,
                PublicKey = task.PublicKey,
                AssignedOn = task.AssignedOn
            }, topic);
        }

        /// <summary>
        /// Send CUD task event
        /// </summary>
        private Task SendCUDAsync(string messageType, PopugTask task, string topic)
        {
            return _producer.ProduceAsync(new
            {
                Type = messageType,
                At = DateTime.UtcNow,
                Task = new TaskMsg(task)
            }, topic);
        }
    }
}

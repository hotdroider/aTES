using Microsoft.EntityFrameworkCore;

namespace aTES.Tasks.Data
{
    public class TasksDbContext : DbContext
    {
        public TasksDbContext(DbContextOptions<TasksDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<PopugTask> Tasks { get; set; }

        public DbSet<Account> Accounts { get; set; }
    }
}

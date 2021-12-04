using Microsoft.EntityFrameworkCore;

namespace aTES.Analytics.Data
{
    public class AnalyticsDbContext : DbContext
    {
        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<TransactionHistory> Transactions { get; set; }
    }
}

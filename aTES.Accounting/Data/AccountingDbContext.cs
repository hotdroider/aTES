using Microsoft.EntityFrameworkCore;

namespace aTES.Accounting.Data
{
    public class AccountingDbContext : DbContext
    {
        public AccountingDbContext(DbContextOptions<AccountingDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<PopugTask> Tasks { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<BillingCycle> BillingCycles { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transaction>(tran =>
            {
                tran.HasOne(t => t.BillingCycle)
                   .WithMany(p => p.Transactions)
                   .HasForeignKey(d => d.BillingCycleId);
            });


        }
    }
}

using aTES.Analytics.Data;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;

namespace aTES.Analytics.Services
{
    public class AnalyticsService
    {
        private readonly AnalyticsDbContext _analyticsDbContext;

        public AnalyticsService(AnalyticsDbContext db)
        {
            _analyticsDbContext = db;
        }

        /// <summary>
        /// Calc popugs with negative balance today
        /// </summary>
        /// <returns></returns>
        public Task<int> GetMinusPopugsAsync()
        {
            return _analyticsDbContext
                .Transactions
                .Where(t => t.Date > DateTime.UtcNow.Date && t.Date < DateTime.UtcNow.AddDays(1).Date && t.Type != TransactionType.Payment)
                .GroupBy(t => t.AccountPublicId)
                .CountAsync(g => g.Sum(t => t.Credit - t.Debit) < 0);
        }

        /// <summary>
        /// Calc management earnings for today
        /// </summary>
        /// <returns></returns>
        public Task<decimal> GetTodaysManagementEarningsAsync()
        {
            return _analyticsDbContext
                .Transactions
                .Where(t => t.Date > DateTime.UtcNow.Date && t.Date < DateTime.UtcNow.AddDays(1).Date && t.Type != TransactionType.Payment)
                .SumAsync(t => t.Debit - t.Credit);
        }

        /// <summary>
        /// Most high task award for period
        /// </summary>
        public Task<decimal> GetMostExpensiveTask(int forLastDays)
        {
            return _analyticsDbContext
               .Transactions
               .Where(t => t.Date > DateTime.UtcNow.Date.AddDays(-forLastDays) && t.Type == TransactionType.Credit)
               .Select(t => t.Credit)
               .DefaultIfEmpty()
               .MaxAsync();
        }
    }
}

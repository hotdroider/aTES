using aTES.Accounting.Data;
using aTES.Accounting.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aTES.Accounting.Services
{
    /// <summary>
    /// Reader for billing lists
    /// </summary>
    public class BillingService
    {
        private readonly AccountingDbContext _accountingDbContext;

        public BillingService(AccountingDbContext accountingDbContext)
        {
            _accountingDbContext = accountingDbContext;
        }

        public async Task<decimal> GetCurrentAmount(string publicAccountId)
        {
            var accId = await GetInternalAccountId(publicAccountId);

            return await _accountingDbContext.BillingCycles
                .Where(c => c.AccountId == accId && c.Date == DateTime.Today)
                .Select(c => c.Amount).FirstOrDefaultAsync();
        }

        public async Task<List<BillintRowModel>> GetBillingList(string publicAccountId)
        {
            var accId = await GetInternalAccountId(publicAccountId);

            return await _accountingDbContext.BillingCycles
                .Where(c => c.AccountId == accId && c.Date == DateTime.Today)
                .SelectMany(c => c.Transactions
                    .Where(t => t.Type != TransactionType.Init)
                    .Select(t => new BillintRowModel()
                    {
                        Amount = t.Amount,
                        TransactionType = t.Type,
                        TaskDescription = t.Task == null 
                        ? string.Empty
                        : t.Task.JiraId + " " + t.Task.Name + " " + t.Task.Description
                    })).ToListAsync();
        }

        private Task<int> GetInternalAccountId(string publicAccountId) => 
            _accountingDbContext.Accounts
                .Where(a => a.PublicKey == publicAccountId)
                .Select(a => a.Id).FirstOrDefaultAsync();

    }
}

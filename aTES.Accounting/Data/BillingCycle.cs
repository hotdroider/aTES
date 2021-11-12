using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aTES.Accounting.Data
{
    /// <summary>
    /// Account cycle
    /// </summary>
    [Index(nameof(Date))]
    public class BillingCycle
    {
        [Key]
        public int Id { get; set; }

        public int AccountId { get; set; }

        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        public BillingCycleState State { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = new HashSet<Transaction>();
    }
}

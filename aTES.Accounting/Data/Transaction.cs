using System;
using System.ComponentModel.DataAnnotations;

namespace aTES.Accounting.Data
{
    /// <summary>
    /// Detailed info on popugs activity
    /// </summary>
    public class Transaction
    {
        [Key]
        public long Id { get; set; }

        public int BillingCycleId { get; set; }

        public BillingCycle BillingCycle { get; set; }

        public int? TaskId { get; set; }

        public PopugTask Task { get; set; }

        public TransactionType Type { get; set; }

        public DateTime Date { get; set; }

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }
    }
}

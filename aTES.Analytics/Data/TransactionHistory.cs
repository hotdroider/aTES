using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace aTES.Analytics.Data
{
    /// <summary>
    /// Historical transaction
    /// </summary>
    [Index(nameof(Date), nameof(AccountPublicId))]
    public class TransactionHistory
    {
        [Key]
        public long Id { get; set; }

        public string AccountPublicId { get; set; }

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }

        public DateTime Date { get; set; }

        public string Reason { get; set; }

        public TransactionType Type { get; set; }
    }
}

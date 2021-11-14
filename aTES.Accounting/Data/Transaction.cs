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

        /// <summary>
        /// Tran amount, always positive number, use type to determine its value for various sides
        /// </summary>
        public decimal Amount { get; set; }
    }
}

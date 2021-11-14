using aTES.Accounting.Data;

namespace aTES.Accounting.Models
{
    public class BillintRowModel
    {
        public TransactionType TransactionType { get; set; }

        public decimal Amount { get; set; }

        public string TaskDescription { get; set;}
    }
}

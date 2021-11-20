using aTES.Accounting.Data;
using System;

namespace aTES.Accounting.Models
{
    public class BillintRowModel
    {
        public TransactionType TransactionType { get; set; }

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }

        public string TaskDescription { get; set;}
    }
}

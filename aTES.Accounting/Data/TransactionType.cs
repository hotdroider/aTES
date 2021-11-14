namespace aTES.Accounting.Data
{
    /// <summary>
    /// Transaction means
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Payment cycle initial balance
        /// </summary>
        Init,

        /// <summary>
        /// Award account
        /// </summary>
        Credit,
        
        /// <summary>
        /// Charge account
        /// </summary>
        Debit,
        
        /// <summary>
        /// Total cycle payment
        /// </summary>
        Payment,
    }
}

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace aTES.PaymentProcessor.Data
{
    [Index(nameof(PublicKey))]
    public class Account
    {
        /// <summary>
        /// Internal PaymentProcessor service id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Public account key inside Popug.INC domain
        /// </summary>
        public string PublicKey { get; set; }

        public string Email { get; set; }

    }
}

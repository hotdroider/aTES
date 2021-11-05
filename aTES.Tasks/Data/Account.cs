using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace aTES.Tasks.Data
{
    /// <summary>
    /// Replication of task accounts
    /// </summary>
    [Index(nameof(PublicKey))]
    public class Account
    {
        /// <summary>
        /// Internal task service id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Public account key inside Popug.INC domain
        /// </summary>
        public string PublicKey { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Role { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}

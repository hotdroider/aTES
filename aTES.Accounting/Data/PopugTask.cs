using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace aTES.Accounting.Data
{
    public class PopugTask
    {
        /// <summary>
        /// Internal task service id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Public task key inside Popug.INC domain
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Task name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Task details
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Jira task id
        /// </summary>
        public string JiraId { get; set; } //ala UBERPOP-42

        /// <summary>
        /// Charge amount for this task assignement
        /// </summary>
        public decimal? AssignFee { get; set; }
    }
}

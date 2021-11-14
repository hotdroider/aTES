using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace aTES.Tasks.Data
{
    [Index(nameof(AssignedOn))]
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
        /// Public account key of task assignee
        /// </summary>
        public string AssignedOn { get; set; }

        /// <summary>
        /// Task open or completed
        /// </summary>
        public TaskState Status { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace aTES.Tasks.Models
{
    public class AddTask
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public string JiraId { get; set; }
    }
}
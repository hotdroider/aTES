using aTES.Tasks.Data;

namespace aTES.Tasks.Models
{
    /// <summary>
    /// Task message format
    /// </summary>
    public class TaskMsg
    {
        public TaskMsg(PopugTask task)
        {
            PublicKey = task.PublicKey;
            Name = task.Name;
            Description = task.Description;
            Status = task.Status.ToString();
            AssignedOn = task.AssignedOn;
        }

        public string PublicKey { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string AssignedOn { get; set; }

        public string Status { get; set; }
    }
}

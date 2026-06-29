using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileAi.Domain.Models
{
    /// <summary>
    /// Represents a task or bug within a User Story on the Kanban board.
    /// Issues are the atomic units of work assigned to team members.
    /// </summary>
    public class Issue
    {
        /// <summary>Unique identifier for the issue.</summary>
        [Key]
        public Guid IssueId { get; set; } = Guid.NewGuid();

        /// <summary>Short, descriptive title of the issue.</summary>
        public string Title { get; set; }

        /// <summary>Current status of the issue within the Kanban workflow.</summary>
        public ItemStatus Status { get; set; }

        /// <summary>Display order used for Drag &amp; Drop sequencing on the board.</summary>
        public int Order { get; set; } // For Drag & Drop sequence [cite: 29]

        // Relations
        /// <summary>Foreign key referencing the parent User Story.</summary>
        public Guid UserStoryId { get; set; }
        public UserStory UserStory { get; set; }

        /// <summary>ID of the user assigned to this issue. Auto-assigned via AI when null.</summary>
        public Guid? AssigneeId { get; set; } // Auto-assigned via IA [cite: 202]

        public ICollection<SubTask> SubTasks { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Attachment> Attachments { get; set; }

        /// <summary>Soft-delete flag. When true, the issue is hidden from all queries.</summary>
        public bool isDeleted { get; set; } = false;

    }
}

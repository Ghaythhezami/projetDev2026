using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileAi.Domain.Models
{
    /// <summary>
    /// Represents a User Story in the Agile backlog.
    /// A User Story captures a feature or requirement from the end-user's perspective,
    /// and is broken down into Issues for implementation.
    /// </summary>
    public class UserStory
    {
        /// <summary>Unique identifier for the User Story.</summary>
        [Key]
        public Guid UserStoryId { get; set; } = Guid.NewGuid();

        /// <summary>Short descriptive title of the User Story.</summary>
        public string Title { get; set; }

        /// <summary>Detailed description generated via NLP processing.</summary>
        public string Description { get; set; } // Generated via NLP [cite: 206]

        /// <summary>Estimated effort in story points. Used for velocity calculation.</summary>
        public int StoryPoints { get; set; }

        /// <summary>Priority level predicted by the ML.NET model.</summary>
        public ItemPriority Priority { get; set; } // Predicted via ML.NET [cite: 205]

        /// <summary>MoSCoW prioritization classification for this story.</summary>
        public MoSCoW MoSCoW { get; set; }

        // Relations
        /// <summary>Foreign key referencing the parent Epic.</summary>
        public Guid EpicId { get; set; }
        public Epic Epic { get; set; }

        /// <summary>Foreign key to the assigned sprint. Null if in the Product Backlog.</summary>
        public Guid? SprintId { get; set; } // Nullable if in Product Backlog
        public Sprint? Sprint { get; set; }

        /// <summary>Current workflow status of the User Story within the sprint.</summary>
        public SprintStatus Status { get; set; } // Added field

        public ICollection<Issue> Issues { get; set; }

        /// <summary>Soft-delete flag. When true, the story is excluded from all active queries.</summary>
        public bool isDeleted { get; set; } = false;

    }

    /// <summary>Defines the priority levels for User Stories and Issues.</summary>
    public enum ItemPriority 
    { 
        Low=1,
        Medium=2,
        High=3,
        Critical=4
    }

    /// <summary>MoSCoW method for requirements prioritization.</summary>
    public enum MoSCoW
    {
        Must = 1,
        Should = 2,
        Could = 3,
        Wont = 4 
    }

    /// <summary>Tracks the progression status of a User Story through the sprint lifecycle.</summary>
    public enum SprintStatus
    {
        Todo = 1,
        InProgress = 2,
        InReview = 3,
        Done = 4
    }
}

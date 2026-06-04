using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileAi.Domain.Dto
{
    public class TeamProjectDto
    {
        public Guid ProjectId { get; set; }
    public string Name { get; set; }
    public string Key { get; set; } // e.g., "PROJ"
    public string Description { get; set; }
    public Guid OwnerId { get; set; }
}

public class ProjectMemberDto {
    public Guid ProjectId { get; set; }
    public Guid MemberId { get; set; }
    public string Role { get; set; } // "ScrumMaster", "PO", "Dev"
}
    public class ProjectDto
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public Guid OwnerId { get; set; }
        public bool IsFinished { get; set; }
        public int TotalCompletedPoints { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
    public class UserDto
    {
        public Guid UserId { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } // e.g., "admin", "product owner", "developer"
    }
}

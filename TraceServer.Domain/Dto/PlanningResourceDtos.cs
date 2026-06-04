using System;
using System.ComponentModel.DataAnnotations;
using AgileAi.Domain.Models;

namespace AgileAi.Domain.Dto
{
    public class CreateEpicDto
    {
        [Required]
        [StringLength(160, MinimumLength = 2)]
        public string Title { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        public Guid ProjectId { get; set; }
    }

    public class UpdateEpicDto : CreateEpicDto { }

    public class EpicResponseDto
    {
        public Guid EpicId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid ProjectId { get; set; }
    }

    public class CreateAcceptanceCriterionDto
    {
        [Required]
        [StringLength(1000, MinimumLength = 2)]
        public string Description { get; set; }

        [Required]
        public Guid UserStoryId { get; set; }
    }

    public class UpdateAcceptanceCriterionDto : CreateAcceptanceCriterionDto
    {
        public bool IsSatisfied { get; set; }
    }

    public class AcceptanceCriterionResponseDto
    {
        public Guid CriterionId { get; set; }
        public string Description { get; set; }
        public bool IsSatisfied { get; set; }
        public Guid UserStoryId { get; set; }
    }

    public class CreateScrumCeremonyDto
    {
        [Required]
        [StringLength(80, MinimumLength = 2)]
        public string Type { get; set; }

        [StringLength(2000)]
        public string Notes { get; set; }

        [StringLength(2000)]
        public string Obstacles { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid SprintId { get; set; }
    }

    public class UpdateScrumCeremonyDto : CreateScrumCeremonyDto { }

    public class ScrumCeremonyResponseDto
    {
        public Guid CeremonyId { get; set; }
        public string Type { get; set; }
        public string Notes { get; set; }
        public string Obstacles { get; set; }
        public DateTime Date { get; set; }
        public Guid SprintId { get; set; }
    }

    public class CreateKanbanColumnDto
    {
        [Required]
        public ItemStatus Status { get; set; }

        [Range(0, int.MaxValue)]
        public int WipLimit { get; set; }

        [Required]
        public Guid ProjectId { get; set; }
    }

    public class UpdateKanbanColumnDto : CreateKanbanColumnDto { }

    public class KanbanColumnResponseDto
    {
        public Guid ColumnId { get; set; }
        public ItemStatus Status { get; set; }
        public int WipLimit { get; set; }
        public Guid ProjectId { get; set; }
    }

    public class UpdateUserDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Nom { get; set; }

        [Required]
        public string Prenom { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string MotDePasse { get; set; }

        public string Telephone { get; set; }

        [Required]
        public string Role { get; set; }
    }
}

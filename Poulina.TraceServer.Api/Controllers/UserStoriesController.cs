using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgileAi.Api.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using AgileAi.Data.Context;
using AgileAi.Domain.Commands;
using AgileAi.Domain.Dto;
using AgileAi.Domain.Models;
using AgileAi.Domain.Queries;

namespace AgileAi.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserStoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly AppDbContext _context;

        public UserStoriesController(
            IMediator mediator,
            IProjectAuthorizationService projectAuthorization,
            AppDbContext context)
        {
            _mediator = mediator;
            _projectAuthorization = projectAuthorization;
            _context = context;
        }

        [HttpGet("backlog/{projectId}")]
        public async Task<IActionResult> GetProductBacklog(Guid projectId)
        {
            if (!await _projectAuthorization.CanAccessProject(projectId))
                return Forbid();

            var query = new GetListGenericQuery<UserStory>(us => us.SprintId == null && us.Epic.ProjectId == projectId);
            var result = await _mediator.Send(query);
            return Ok(result.Select(ToResponse));
        }

        [HttpGet("sprint/{sprintId}")]
        public async Task<IActionResult> GetSprintStories(Guid sprintId)
        {
            if (!await _projectAuthorization.CanAccessSprint(sprintId))
                return Forbid();

            var query = new GetListGenericQuery<UserStory>(us => us.SprintId == sprintId);
            var result = await _mediator.Send(query);
            return Ok(result.Select(ToResponse));
        }

        /// <summary>
        /// Creates a new User Story and associates it with an Epic.
        /// Validates story parameters and authorization.
        /// </summary>
        /// <param name="request">The creation payload containing title, epic, story points, priority, etc.</param>
        /// <returns>The created User Story response.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateUserStory([FromBody] CreateUserStoryDto request)
        {
            if (request == null)
                return BadRequest(new ApiErrorResponse { Message = "Request cannot be null.", Code = "NULL_REQUEST" });

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new ApiErrorResponse { Message = "User Story title is required.", Code = "EMPTY_TITLE" });

            if (request.StoryPoints < 0)
                return BadRequest(new ApiErrorResponse { Message = "Story points cannot be negative.", Code = "INVALID_STORY_POINTS" });

            var projectId = await _context.Epics
                .Where(e => e.EpicId == request.EpicId)
                .Select(e => (Guid?)e.ProjectId)
                .FirstOrDefaultAsync();

            if (!projectId.HasValue)
                return BadRequest(new { Message = "Invalid epic." });

            if (!await _projectAuthorization.CanManageProject(projectId.Value))
                return Forbid();

            var result = await _mediator.Send(new CreateUserStoryCommand(
                request.Title,
                request.Description,
                request.StoryPoints,
                request.Priority,
                request.MoSCoW,
                request.EpicId,
                request.SprintId,
                request.Status));

            return Ok(ToResponse(result));
        }

        /// <summary>
        /// Assigns a User Story to a specific Sprint, or moves it to the Product Backlog if sprintId is null.
        /// </summary>
        /// <param name="id">The unique identifier of the User Story.</param>
        /// <param name="sprintId">The unique identifier of the target Sprint (optional).</param>
        [HttpPatch("{id}/sprint")]
        public async Task<IActionResult> AssignToSprint(Guid id, [FromBody] Guid? sprintId)
        {
            if (!await _projectAuthorization.CanAccessUserStory(id))
                return Forbid();

            if (sprintId.HasValue && !await _projectAuthorization.CanAccessSprint(sprintId.Value))
                return Forbid();

            var result = await _mediator.Send(new AssignUserStoryToSprintCommand(id, sprintId));
            return result != null ? Ok(ToResponse(result)) : NotFound();
        }

        private static UserStoryResponseDto ToResponse(UserStory story)
        {
            return new UserStoryResponseDto
            {
                UserStoryId = story.UserStoryId,
                Title = story.Title,
                Description = story.Description,
                StoryPoints = story.StoryPoints,
                Priority = story.Priority,
                MoSCoW = story.MoSCoW,
                EpicId = story.EpicId,
                SprintId = story.SprintId,
                Status = story.Status
            };
        }
    }
}

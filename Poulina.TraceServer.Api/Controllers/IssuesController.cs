using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AgileAi.Api.Hubs;
using AgileAi.Api.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using AgileAi.Data.Context;
using AgileAi.Domain.Commands;
using AgileAi.Domain.Dto;
using AgileAi.Domain.Handlers;
using AgileAi.Domain.Models;
using AgileAi.Domain.Queries;

namespace AgileAi.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class IssuesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly AppDbContext _context;
        private readonly IActivityService _activityService;
        private readonly IHubContext<BoardHub> _boardHub;

        public IssuesController(
            IMediator mediator,
            ICurrentUserService currentUser,
            IProjectAuthorizationService projectAuthorization,
            AppDbContext context,
            IActivityService activityService,
            IHubContext<BoardHub> boardHub)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _projectAuthorization = projectAuthorization;
            _context = context;
            _activityService = activityService;
            _boardHub = boardHub;
        }

        /// <summary>
        /// Retrieves the Kanban board for a specific sprint, including all associated issues.
        /// </summary>
        /// <param name="sprintId">The unique identifier of the sprint.</param>
        /// <returns>A list of issues grouped for Kanban display.</returns>
        [HttpGet("board/{sprintId}")]
        public async Task<IActionResult> GetKanbanBoard(Guid sprintId)
        {
            if (!await _projectAuthorization.CanAccessSprint(sprintId))
                return Forbid();

            var query = new GetListGenericQuery<Issue>(i => i.UserStory.SprintId == sprintId);
            var result = await _mediator.Send(query);
            return Ok(result.Select(ToResponse));
        }

        /// <summary>
        /// Retrieves all pending tasks assigned to the currently authenticated user.
        /// Excludes issues with status 'Done'.
        /// </summary>
        /// <returns>A list of active issues assigned to the current user.</returns>
        [HttpGet("my-tasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            var userId = _currentUser.UserId;
            var query = new GetListGenericQuery<Issue>(i => i.AssigneeId == userId && i.Status != ItemStatus.Done);
            var result = await _mediator.Send(query);
            return Ok(result.Select(ToResponse));
        }

        /// <summary>
        /// Creates a new issue under a specified User Story.
        /// Notifies the assignee and broadcasts the change via SignalR.
        /// </summary>
        /// <param name="request">The issue creation payload.</param>
        /// <returns>The created issue object.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateIssue([FromBody] CreateIssueDto request)
        {
            if (request == null)
                return BadRequest(new ApiErrorResponse { Message = "Request body cannot be null.", Code = "NULL_REQUEST" });

            if (!await _projectAuthorization.CanAccessUserStory(request.UserStoryId))
                return Forbid();

            var result = await _mediator.Send(new CreateIssueCommand(
                request.Title,
                request.Order,
                request.UserStoryId,
                request.AssigneeId));

            await LogIssueActivity(result, "IssueCreated");
            await NotifyAssignee(result, "You were assigned to a new issue.");
            await _boardHub.Clients.Group(result.UserStoryId.ToString()).SendAsync("IssueChanged", ToResponse(result));

            return Ok(ToResponse(result));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIssue(Guid id, [FromBody] UpdateIssueDto request)
        {
            if (request == null)
                return BadRequest();

            if (!await _projectAuthorization.CanAccessIssue(id))
                return Forbid();

            if (!await _projectAuthorization.CanAccessUserStory(request.UserStoryId))
                return Forbid();

            var existingIssue = await _mediator.Send(new GetGenericQuery<Issue>(i => i.IssueId == id));

            if (existingIssue == null)
                return NotFound();

            if (!Enum.IsDefined(typeof(ItemStatus), request.Status))
                return BadRequest(new ApiErrorResponse
                {
                    Message = "Invalid issue status.",
                    Code = "INVALID_ISSUE_STATUS"
                });

            if (!WorkflowValidation.IsValidIssueStatusTransition(existingIssue.Status, request.Status))
                return BadRequest(new ApiErrorResponse
                {
                    Message = "Invalid issue status transition.",
                    Code = "INVALID_ISSUE_STATUS_TRANSITION"
                });

            var issue = new Issue
            {
                IssueId = id,
                Title = request.Title,
                Status = request.Status,
                Order = request.Order,
                UserStoryId = request.UserStoryId,
                AssigneeId = request.AssigneeId
            };

            var result = await _mediator.Send(new PutGenericCommand<Issue>(id, issue));
            await LogIssueActivity(result, "IssueUpdated");
            await NotifyAssignee(result, "An issue assigned to you was updated.");
            await _boardHub.Clients.Group(result.UserStoryId.ToString()).SendAsync("IssueChanged", ToResponse(result));
            return Ok(ToResponse(result));
        }

        [HttpPatch("{id}/move")]
        public async Task<IActionResult> MoveIssue(Guid id, [FromBody] MoveIssueDto request)
        {
            if (request == null)
                return BadRequest();

            if (!await _projectAuthorization.CanAccessIssue(id))
                return Forbid();

            var result = await _mediator.Send(new MoveIssueStatusCommand(id, request.Status, request.Order));
            if (result != null)
            {
                await LogIssueActivity(result, "IssueMoved");
                await _boardHub.Clients.Group(result.UserStoryId.ToString()).SendAsync("IssueMoved", ToResponse(result));
            }
            return result != null ? Ok(ToResponse(result)) : NotFound();
        }

        /// <summary>
        /// Manually assigns or unassigns a user to/from an issue.
        /// Sends a notification to the new assignee if applicable.
        /// </summary>
        /// <param name="id">The unique identifier of the issue.</param>
        /// <param name="assigneeId">The user ID to assign, or null to unassign.</param>
        [HttpPatch("{id}/assign")]
        public async Task<IActionResult> AssignIssue(Guid id, [FromBody] Guid? assigneeId)
        {
            if (!await _projectAuthorization.CanAccessIssue(id))
                return Forbid();

            var result = await _mediator.Send(new AssignIssueCommand(id, assigneeId));
            if (result != null)
            {
                await LogIssueActivity(result, "IssueAssigned");
                await NotifyAssignee(result, "You were assigned to an issue.");
                await _boardHub.Clients.Group(result.UserStoryId.ToString()).SendAsync("IssueChanged", ToResponse(result));
            }
            return result != null ? Ok(ToResponse(result)) : NotFound();
        }

        /// <summary>
        /// Triggers the AI-based auto-assignment algorithm for a given issue.
        /// The system selects the most suitable team member based on workload and skills.
        /// </summary>
        /// <param name="id">The unique identifier of the issue to auto-assign.</param>
        [HttpPost("{id}/auto-assign")]
        public async Task<IActionResult> AutoAssignIssue(Guid id)
        {
            if (!await _projectAuthorization.CanAccessIssue(id))
                return Forbid();

            var result = await _mediator.Send(new AutoAssignCommand(id));
            if (result != null)
            {
                await LogIssueActivity(result, "IssueAutoAssigned");
                await NotifyAssignee(result, "An issue was auto-assigned to you.");
                await _boardHub.Clients.Group(result.UserStoryId.ToString()).SendAsync("IssueChanged", ToResponse(result));
            }

            return result != null ? Ok(ToResponse(result)) : NotFound();
        }

        private static IssueResponseDto ToResponse(Issue issue)
        {
            return new IssueResponseDto
            {
                IssueId = issue.IssueId,
                Title = issue.Title,
                Status = issue.Status,
                Order = issue.Order,
                UserStoryId = issue.UserStoryId,
                AssigneeId = issue.AssigneeId
            };
        }

        private async Task LogIssueActivity(Issue issue, string action)
        {
            var projectId = await _context.Issues
                .Where(i => i.IssueId == issue.IssueId)
                .Select(i => (Guid?)i.UserStory.Epic.ProjectId)
                .FirstOrDefaultAsync();

            if (projectId.HasValue)
                await _activityService.Log(projectId.Value, action, nameof(Issue), issue.IssueId);
        }

        private async Task NotifyAssignee(Issue issue, string message)
        {
            if (issue.AssigneeId.HasValue && issue.AssigneeId.Value != _currentUser.UserId)
                await _activityService.Notify(issue.AssigneeId.Value, message, $"/issues/{issue.IssueId}");
        }
    }
}

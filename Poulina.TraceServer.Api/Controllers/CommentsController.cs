using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AgileAi.Api.Hubs;
using AgileAi.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class CommentsController : ControllerBase
    {
        private static readonly Regex MentionRegex = new Regex(@"@\[(?<email>[^\]\s]+@[^\]\s]+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly AppDbContext _context;
        private readonly IActivityService _activityService;
        private readonly IHubContext<BoardHub> _boardHub;

        public CommentsController(
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

        [HttpGet("issue/{issueId}")]
        public async Task<IActionResult> GetCommentsForIssue(Guid issueId)
        {
            if (!await _projectAuthorization.CanAccessIssue(issueId))
                return Forbid();

            var query = new GetListGenericQuery<Comment>(c => c.IssueId == issueId);
            var result = await _mediator.Send(query);
            return Ok(result.Select(comment => ToResponse(comment)));
        }

        /// <summary>
        /// Adds a new comment to a specific issue.
        /// Performs input validation and parses mentions to alert users.
        /// </summary>
        /// <param name="request">The comment creation data.</param>
        /// <returns>The created comment with resolved mentions.</returns>
        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] CreateCommentDto request)
        {
            if (request == null)
                return BadRequest(new ApiErrorResponse { Message = "Comment request cannot be null.", Code = "NULL_REQUEST" });

            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new ApiErrorResponse { Message = "Comment content cannot be empty.", Code = "EMPTY_COMMENT_CONTENT" });

            if (request.Content.Length > 2000)
                return BadRequest(new ApiErrorResponse { Message = "Comment content cannot exceed 2000 characters.", Code = "COMMENT_TOO_LONG" });

            if (!await _projectAuthorization.CanAccessIssue(request.IssueId))
                return Forbid();

            var comment = new Comment
            {
                CommentId = Guid.NewGuid(),
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IssueId = request.IssueId,
                AuthorId = _currentUser.UserId
            };

            var result = await _mediator.Send(new AddGenericCommand<Comment>(comment));
            var projectId = await GetProjectIdForIssue(result.IssueId);
            var mentionedUserIds = projectId.HasValue
                ? await ResolveMentionedUsers(projectId.Value, request.Content)
                : new List<Guid>();

            await LogCommentActivity(result, projectId);
            await NotifyMentionedUsers(mentionedUserIds, result);

            var response = ToResponse(result, mentionedUserIds);
            await _boardHub.Clients.Group(result.IssueId.ToString()).SendAsync("CommentAdded", response);
            return Ok(response);
        }

        private async Task<Guid?> GetProjectIdForIssue(Guid issueId)
        {
            return await _context.Issues
                .Where(i => i.IssueId == issueId)
                .Select(i => (Guid?)i.UserStory.Epic.ProjectId)
                .FirstOrDefaultAsync();
        }

        private async Task LogCommentActivity(Comment comment, Guid? projectId)
        {
            if (projectId.HasValue)
                await _activityService.Log(projectId.Value, "CommentAdded", nameof(Comment), comment.CommentId);
        }

        private async Task<List<Guid>> ResolveMentionedUsers(Guid projectId, string content)
        {
            var emails = MentionRegex.Matches(content ?? string.Empty)
                .Select(match => match.Groups["email"].Value.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!emails.Any())
                return new List<Guid>();

            return await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId && emails.Contains(pm.Member.Email))
                .Select(pm => pm.MemberId)
                .Where(memberId => memberId != _currentUser.UserId)
                .Distinct()
                .ToListAsync();
        }

        private async Task NotifyMentionedUsers(IEnumerable<Guid> mentionedUserIds, Comment comment)
        {
            foreach (var userId in mentionedUserIds)
                await _activityService.Notify(userId, "You were mentioned in a comment.", $"/issues/{comment.IssueId}");
        }

        private static CommentResponseDto ToResponse(Comment comment, IEnumerable<Guid> mentionedUserIds = null)
        {
            return new CommentResponseDto
            {
                CommentId = comment.CommentId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IssueId = comment.IssueId,
                AuthorId = comment.AuthorId,
                MentionedUserIds = mentionedUserIds ?? Array.Empty<Guid>()
            };
        }
    }

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubTasksController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly AppDbContext _context;
        private readonly IActivityService _activityService;
        private readonly IHubContext<BoardHub> _boardHub;

        public SubTasksController(
            IMediator mediator,
            IProjectAuthorizationService projectAuthorization,
            AppDbContext context,
            IActivityService activityService,
            IHubContext<BoardHub> boardHub)
        {
            _mediator = mediator;
            _projectAuthorization = projectAuthorization;
            _context = context;
            _activityService = activityService;
            _boardHub = boardHub;
        }

        /// <summary>
        /// Creates a new subtask under an existing issue.
        /// </summary>
        /// <param name="request">The subtask creation data.</param>
        [HttpPost]
        public async Task<IActionResult> AddSubTask([FromBody] CreateSubTaskDto request)
        {
            if (request == null)
                return BadRequest(new ApiErrorResponse { Message = "Request cannot be null.", Code = "NULL_REQUEST" });

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new ApiErrorResponse { Message = "Subtask title is required.", Code = "EMPTY_SUBTASK_TITLE" });

            if (!await _projectAuthorization.CanAccessIssue(request.IssueId))
                return Forbid();

            var subTask = new SubTask
            {
                SubTaskId = Guid.NewGuid(),
                Title = request.Title,
                IssueId = request.IssueId,
                IsCompleted = false
            };

            var result = await _mediator.Send(new AddGenericCommand<SubTask>(subTask));
            await LogSubTaskActivity(result, "SubTaskCreated");
            await _boardHub.Clients.Group(result.IssueId?.ToString() ?? string.Empty).SendAsync("SubTaskChanged", ToResponse(result));
            return Ok(ToResponse(result));
        }

        [HttpPut("{id}/toggle")]
        public async Task<IActionResult> ToggleSubTask(Guid id, [FromBody] UpdateSubTaskDto request)
        {
            if (request == null)
                return BadRequest();

            if (!await _projectAuthorization.CanAccessSubTask(id))
                return Forbid();

            var result = await _mediator.Send(new ToggleSubTaskCommand(
                id,
                request.Title,
                request.IsCompleted,
                request.IssueId));

            if (result != null)
            {
                await LogSubTaskActivity(result, "SubTaskUpdated");
                await _boardHub.Clients.Group(result.IssueId?.ToString() ?? string.Empty).SendAsync("SubTaskChanged", ToResponse(result));
            }

            return result != null ? Ok(ToResponse(result)) : NotFound();
        }

        private async Task LogSubTaskActivity(SubTask subTask, string action)
        {
            var projectId = await _context.SubTasks
                .Where(st => st.SubTaskId == subTask.SubTaskId && st.IssueId.HasValue)
                .Select(st => (Guid?)st.Issue.UserStory.Epic.ProjectId)
                .FirstOrDefaultAsync();

            if (projectId.HasValue)
                await _activityService.Log(projectId.Value, action, nameof(SubTask), subTask.SubTaskId);
        }

        private static SubTaskResponseDto ToResponse(SubTask subTask)
        {
            return new SubTaskResponseDto
            {
                SubTaskId = subTask.SubTaskId,
                Title = subTask.Title,
                IsCompleted = subTask.IsCompleted,
                IssueId = subTask.IssueId
            };
        }
    }
}

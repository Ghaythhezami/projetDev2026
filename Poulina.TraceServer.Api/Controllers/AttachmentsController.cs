using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgileAi.Api.Hubs;
using AgileAi.Api.Services;
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
    public class AttachmentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly IActivityService _activityService;
        private readonly IHubContext<BoardHub> _boardHub;
        private readonly IWebHostEnvironment _environment;
        private readonly AppDbContext _context;

        public AttachmentsController(
            IMediator mediator,
            ICurrentUserService currentUser,
            IProjectAuthorizationService projectAuthorization,
            IActivityService activityService,
            IHubContext<BoardHub> boardHub,
            IWebHostEnvironment environment,
            AppDbContext context)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _projectAuthorization = projectAuthorization;
            _activityService = activityService;
            _boardHub = boardHub;
            _environment = environment;
            _context = context;
        }

        /// <summary>
        /// Retrieves all attachments linked to a specific issue.
        /// </summary>
        /// <param name="issueId">The unique identifier of the issue.</param>
        /// <returns>A list of issue attachments.</returns>
        [HttpGet("issue/{issueId}")]
        public async Task<IActionResult> GetForIssue(Guid issueId)
        {
            if (!await _projectAuthorization.CanAccessIssue(issueId))
                return Forbid();

            var attachments = await _mediator.Send(new GetListGenericQuery<Attachment>(a => a.IssueId == issueId));
            return Ok(attachments.Select(ToResponse));
        }

        /// <summary>
        /// Uploads a file attachment for a given issue.
        /// Restricts file size to a maximum of 25MB.
        /// </summary>
        /// <param name="issueId">The unique identifier of the target issue.</param>
        /// <param name="file">The multipart file payload.</param>
        [HttpPost("issue/{issueId}")]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> Upload(Guid issueId, [FromForm] IFormFile file)
        {
            if (!await _projectAuthorization.CanAccessIssue(issueId))
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest(new ApiErrorResponse
                {
                    Message = "A file is required and cannot be empty.",
                    Code = "FILE_REQUIRED"
                });

            var uploadsDirectory = Path.Combine(_environment.ContentRootPath, "uploads", "attachments");
            Directory.CreateDirectory(uploadsDirectory);

            var sanitizedFileName = Path.GetFileName(file.FileName);
            var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(sanitizedFileName)}";
            var physicalPath = Path.Combine(uploadsDirectory, storedFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new Attachment
            {
                AttachmentId = Guid.NewGuid(),
                FileName = sanitizedFileName,
                BlobUrl = $"/uploads/attachments/{storedFileName}",
                FileType = file.ContentType,
                FileSize = file.Length,
                IssueId = issueId,
                UploaderId = _currentUser.UserId == Guid.Empty ? null : _currentUser.UserId
            };

            var result = await _mediator.Send(new AddGenericCommand<Attachment>(attachment));
            var projectId = await ResolveProjectId(issueId);
            if (projectId.HasValue)
                await _activityService.Log(projectId.Value, "AttachmentUploaded", nameof(Attachment), result.AttachmentId);
            await _boardHub.Clients.Group(issueId.ToString()).SendAsync("AttachmentAdded", ToResponse(result));

            return Ok(ToResponse(result));
        }

        private async Task<Guid?> ResolveProjectId(Guid issueId)
        {
            return await _context.Issues
                .Where(i => i.IssueId == issueId)
                .Select(i => (Guid?)i.UserStory.Epic.ProjectId)
                .FirstOrDefaultAsync();
        }

        private static AttachmentResponseDto ToResponse(Attachment attachment)
        {
            return new AttachmentResponseDto
            {
                AttachmentId = attachment.AttachmentId,
                FileName = attachment.FileName,
                BlobUrl = attachment.BlobUrl,
                FileType = attachment.FileType,
                FileSize = attachment.FileSize,
                IssueId = attachment.IssueId,
                UploaderId = attachment.UploaderId
            };
        }
    }
}

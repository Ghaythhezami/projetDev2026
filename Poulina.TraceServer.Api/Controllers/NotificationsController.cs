using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgileAi.Api.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using AgileAi.Data.Context;
using AgileAi.Domain.Dto;

namespace AgileAi.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IActivityService _activityService;

        public NotificationsController(
            AppDbContext context,
            ICurrentUserService currentUser,
            IActivityService activityService)
        {
            _context = context;
            _currentUser = currentUser;
            _activityService = activityService;
        }

        /// <summary>
        /// Retrieves all notifications for the currently logged-in user.
        /// Notifications are sorted in descending order of their creation date.
        /// </summary>
        /// <returns>A list of user notifications.</returns>
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var userId = _currentUser.UserId;
            var notifications = await _context.Notifications
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationResponseDto
                {
                    NotificationId = n.NotificationId,
                    Message = n.Message,
                    Link = n.Link,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReceiverId = n.ReceiverId
                })
                .ToListAsync();

            return Ok(notifications);
        }

        /// <summary>
        /// Sandbox/Testing endpoint to trigger manual test notifications.
        /// </summary>
        /// <param name="request">The test notification request model.</param>
        [HttpPost("TTest")]
        public async Task<IActionResult> TTest([FromBody] TestNotificationRequestDto request)
        {
            if (request == null || request.ReceiverId == Guid.Empty)
                return BadRequest(new ApiErrorResponse
                {
                    Message = "ReceiverId is required.",
                    Code = "RECEIVER_ID_REQUIRED"
                });

            var receiverExists = await _context.Users.AnyAsync(u => u.UserId == request.ReceiverId);

            if (!receiverExists)
                return NotFound(new ApiErrorResponse
                {
                    Message = "Receiver user was not found.",
                    Code = "RECEIVER_NOT_FOUND"
                });

            var description = string.IsNullOrWhiteSpace(request.Description)
                ? "TEST"
                : request.Description.Trim();

            await _activityService.Notify(request.ReceiverId, description, "/notifications/test");

            return Ok(new
            {
                receiverId = request.ReceiverId,
                message = description,
                link = "/notifications/test"
            });
        }

        /// <summary>
        /// Dispatches a notification to a specific user.
        /// Validates receiver existence and formats optional redirect links.
        /// </summary>
        /// <param name="request">The notification details.</param>
        [HttpPost("Send")]
        public async Task<IActionResult> Send([FromBody] SendNotificationRequestDto request)
        {
            if (request == null || request.ReceiverId == Guid.Empty)
                return BadRequest(new ApiErrorResponse
                {
                    Message = "ReceiverId is required.",
                    Code = "RECEIVER_ID_REQUIRED"
                });

            if (string.IsNullOrWhiteSpace(request.Description))
                return BadRequest(new ApiErrorResponse
                {
                    Message = "Description is required.",
                    Code = "DESCRIPTION_REQUIRED"
                });

            var receiverExists = await _context.Users.AnyAsync(u => u.UserId == request.ReceiverId);

            if (!receiverExists)
                return NotFound(new ApiErrorResponse
                {
                    Message = "Receiver user was not found.",
                    Code = "RECEIVER_NOT_FOUND"
                });

            var description = request.Description.Trim();
            var link = string.IsNullOrWhiteSpace(request.Link)
                ? "/notifications"
                : request.Link.Trim();

            await _activityService.Notify(request.ReceiverId, description, link);

            return Ok(new
            {
                receiverId = request.ReceiverId,
                message = description,
                link
            });
        }

        /// <summary>
        /// Marks a notification as read.
        /// Only notifications belonging to the current user can be modified.
        /// </summary>
        /// <param name="id">The unique identifier of the notification.</param>
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var userId = _currentUser.UserId;
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.ReceiverId == userId);

            if (notification == null)
                return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}

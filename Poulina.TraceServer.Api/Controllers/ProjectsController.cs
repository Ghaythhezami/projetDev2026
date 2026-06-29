using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgileAi.Api.Services;
using System;
using System.Threading.Tasks;
using AgileAi.Domain.Commands;
using AgileAi.Domain.Dto;
using AgileAi.Domain.Models;
using AgileAi.Domain.Queries;

namespace AgileAi.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly ICurrentUserService _currentUser;

        public ProjectsController(
            IMediator mediator,
            IProjectAuthorizationService projectAuthorization,
            ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _projectAuthorization = projectAuthorization;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Retrieves project metadata by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the project.</param>
        /// <returns>The project response DTO if found and authorized.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await _projectAuthorization.CanAccessProject(id))
                return Forbid();

            var query = new GetGenericQuery<Project>(p => p.ProjectId == id);
            var result = await _mediator.Send(query);

            return result != null ? Ok(ToResponse(result)) : NotFound();
        }

        /// <summary>
        /// Creates a new project and assigns the current user as the project owner.
        /// </summary>
        /// <param name="request">The project creation model containing name, description, and project key.</param>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto request)
        {
            if (request == null)
                return BadRequest(new ApiErrorResponse { Message = "Project data cannot be null.", Code = "NULL_REQUEST" });

            if (string.IsNullOrWhiteSpace(request.ProjectName))
                return BadRequest(new ApiErrorResponse { Message = "Project name is required.", Code = "EMPTY_PROJECT_NAME" });

            if (string.IsNullOrWhiteSpace(request.Key) || request.Key.Length < 2)
                return BadRequest(new ApiErrorResponse { Message = "Project Key must be at least 2 characters.", Code = "INVALID_PROJECT_KEY" });

            var result = await _mediator.Send(new CreateProjectCommand(
                request.ProjectName,
                request.ProjectDescription,
                request.Key,
                _currentUser.UserId));

            return Ok(ToResponse(result));
        }

        private static ProjectResponseDto ToResponse(Project project)
        {
            return new ProjectResponseDto
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                ProjectDescription = project.ProjectDescription,
                Key = project.Key,
                OwnerId = project.OwnerId,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                IsFinished = project.IsFinished,
                FinishedAt = project.FinishedAt,
                TotalCompletedPoints = project.TotalCompletedPoints
            };
        }
    }
}

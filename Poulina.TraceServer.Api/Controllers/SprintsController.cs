using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgileAi.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class SprintsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IProjectAuthorizationService _projectAuthorization;

        public SprintsController(IMediator mediator, IProjectAuthorizationService projectAuthorization)
        {
            _mediator = mediator;
            _projectAuthorization = projectAuthorization;
        }

        /// <summary>
        /// Retrieves all sprints belonging to a specific project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <returns>A list of sprints ordered by creation date.</returns>
        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetSprintsByProject(Guid projectId)
        {
            if (!await _projectAuthorization.CanAccessProject(projectId))
                return Forbid();

            var query = new GetListGenericQuery<Sprint>(s => s.ProjectId == projectId);
            var result = await _mediator.Send(query);
            return Ok(result.Select(ToResponse));
        }

        /// <summary>
        /// Creates a new sprint for a project. The sprint name must be unique within the project.
        /// Start date must be strictly before end date.
        /// </summary>
        /// <param name="request">The sprint creation payload.</param>
        /// <returns>The created sprint object.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateSprint([FromBody] CreateSprintDto request)
        {
            if (request == null)
                return BadRequest(new ApiErrorResponse { Message = "Request body cannot be null.", Code = "NULL_REQUEST" });

            if (!await _projectAuthorization.CanManageProject(request.ProjectId))
                return Forbid();

            if (request.EndDate <= request.StartDate)
                return BadRequest(new ApiErrorResponse
                {
                    Message = "Sprint end date must be after start date.",
                    Code = "INVALID_SPRINT_DATES"
                });

            var result = await _mediator.Send(new CreateSprintCommand(
                request.Name,
                request.StartDate,
                request.EndDate,
                request.ProjectId));

            return Ok(ToResponse(result));
        }

        /// <summary>
        /// Starts a sprint, transitioning it from 'Planned' to 'Active'.
        /// Only one sprint can be active per project at a time.
        /// </summary>
        /// <param name="id">The unique identifier of the sprint to start.</param>
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartSprint(Guid id)
        {
            if (!await _projectAuthorization.CanAccessSprint(id))
                return Forbid();

            var result = await _mediator.Send(new StartSprintCommand(id));
            return result != null ? Ok(ToResponse(result)) : NotFound();
        }

        /// <summary>
        /// Closes an active sprint. Incomplete user stories are moved back to the product backlog.
        /// </summary>
        /// <param name="id">The unique identifier of the sprint to close.</param>
        [HttpPost("{id}/close")]
        public async Task<IActionResult> CloseSprint(Guid id)
        {
            if (!await _projectAuthorization.CanAccessSprint(id))
                return Forbid();

            var result = await _mediator.Send(new CloseSprintCommand(id));
            return result != null ? Ok(ToResponse(result)) : NotFound();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSprint(Guid id, [FromBody] UpdateSprintDto request)
        {
            if (request == null)
                return BadRequest();

            if (!await _projectAuthorization.CanManageProject(request.ProjectId))
                return Forbid();

            if (request.EndDate <= request.StartDate)
                return BadRequest(new ApiErrorResponse
                {
                    Message = "Sprint end date must be after start date.",
                    Code = "INVALID_SPRINT_DATES"
                });

            if (!Enum.IsDefined(typeof(ItemStatus), request.Status))
                return BadRequest(new ApiErrorResponse
                {
                    Message = "Invalid sprint status.",
                    Code = "INVALID_SPRINT_STATUS"
                });

            var sprint = new Sprint
            {
                SprintId = id,
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ProjectId = request.ProjectId,
                Status = request.Status,
                CompletedPoints = request.CompletedPoints
            };

            var result = await _mediator.Send(new PutGenericCommand<Sprint>(id, sprint));
            return Ok(ToResponse(result));
        }

        private static SprintResponseDto ToResponse(Sprint sprint)
        {
            return new SprintResponseDto
            {
                SprintId = sprint.SprintId,
                Name = sprint.Name,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Status = sprint.Status,
                ProjectId = sprint.ProjectId,
                CompletedPoints = sprint.CompletedPoints
            };
        }
    }
}

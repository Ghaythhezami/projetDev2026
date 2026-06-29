using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgileAi.Api.Services;
using System;
using System.Threading.Tasks;

namespace AgileAi.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectExecutionController : ControllerBase
    {
        private readonly IProjectAnalyticsService _analyticsService;
        private readonly IProjectAuthorizationService _projectAuthorization;

        public ProjectExecutionController(
            IProjectAnalyticsService analyticsService,
            IProjectAuthorizationService projectAuthorization)
        {
            _analyticsService = analyticsService;
            _projectAuthorization = projectAuthorization;
        }

        /// <summary>
        /// Finalizes the project and tallies all completed points.
        /// Only accessible by Project Owners or Administrators.
        /// </summary>
        /// <param name="id">The unique identifier of the project.</param>
        [HttpPost("{id}/finalize")]
        public async Task<IActionResult> FinishProject(Guid id)
        {
            if (!await _projectAuthorization.CanManageProject(id))
                return Forbid();

            // Logic: Only the Project Owner or Admin can finalize
            await _analyticsService.FinalizeProject(id);
            return Ok(new { message = "Project finalized and points tallied." });
        }
    }
}

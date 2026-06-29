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
    }
}

using Microsoft.AspNetCore.Mvc;
using ResumeMatcher.Core.Interfaces;
using ResumeMatcher.Core.Models;

namespace ResumeMatcher.API.Controllers
{
    [ApiController]
    [Route("api/resumes")]
    public class ResumeMatchingController : ControllerBase
    {
        private readonly IResumeMatchingService _resumeMatchingService;

        public ResumeMatchingController(IResumeMatchingService resumeMatchingService)
        {
            _resumeMatchingService = resumeMatchingService;
        }

        [HttpPost("fetch")]
        public async Task<ActionResult<ResumeMatchingResponse>> MatchResumes([FromBody] ResumeMatchingRequest request)
        {
            var result = await _resumeMatchingService.MatchResumesAsync(request);
            return Ok(result);
        }
    }
} 
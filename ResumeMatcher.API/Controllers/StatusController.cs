using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ResumeMatcher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StatusController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            var useDotNet = _configuration.GetValue<bool>("ResumeMatcher:UseDotNetLogic");
            return Ok(new
            {
                logic = useDotNet ? ".NET" : "Python",
                useDotNet
            });
        }
    }
} 
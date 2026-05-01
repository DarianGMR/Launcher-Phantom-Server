using Microsoft.AspNetCore.Mvc;
using LauncherPhantomServer.Models;

namespace LauncherPhantomServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LauncherController : ControllerBase
    {
        private readonly ILogger<LauncherController> _logger;

        public LauncherController(ILogger<LauncherController> logger)
        {
            _logger = logger;
        }

        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            _logger.LogInformation("[LauncherController] Solicitud de versión");

            var versionInfo = new VersionInfo
            {
                Version = "0.1.0",
                DownloadUrl = "http://localhost:5000/downloads/launcher-update.exe",
                Changes = "- Versión inicial\n- Autenticación\n- Sistema de bans",
                Required = false
            };

            return Ok(versionInfo);
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            _logger.LogInformation("[LauncherController] Health check");
            return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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

        [HttpGet("health")]
        public IActionResult Health()
        {
            try
            {
                _logger.LogInformation("[LauncherController] Health check request");
                return Ok(new { status = "ok", message = "Server is running" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LauncherController] Error en Health check");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        [HttpGet("version")]
        public IActionResult Version()
        {
            try
            {
                _logger.LogInformation("[LauncherController] Version request");
                return Ok(new 
                { 
                    version = "1.0.0",
                    releaseDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    status = "ok"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LauncherController] Error obteniendo versión");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
    }
}

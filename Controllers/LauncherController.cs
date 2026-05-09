using Microsoft.AspNetCore.Mvc;
using LauncherPhantomServer.Services;

namespace LauncherPhantomServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LauncherController : ControllerBase
    {
        private readonly ILogger<LauncherController> _logger;
        private readonly UpdateService _updateService;

        public LauncherController(ILogger<LauncherController> logger, UpdateService updateService)
        {
            _logger = logger;
            _updateService = updateService;
        }

        [HttpGet("health")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public IActionResult Health()
        {
            try
            {
                _logger.LogDebug("[API] GET /api/launcher/health");
                
                return Ok(new
                {
                    status = "ok",
                    message = "Servidor funcionando correctamente",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] ERROR en GET /api/launcher/health");
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Error interno del servidor"
                });
            }
        }

        [HttpGet("version")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Version()
        {
            try
            {
                _logger.LogDebug("[API] GET /api/launcher/version");
                
                var updateInfo = await _updateService.GetUpdateInfoAsync();
                
                if (updateInfo == null)
                {
                    _logger.LogError("[API] ERROR: No se pudo leer la información de actualización");
                    return StatusCode(500, new { status = "error", message = "No se pudo leer la información de actualización" });
                }

                _logger.LogInformation($"[API] Versión actual enviada: {updateInfo.Version}");

                return Ok(new
                {
                    version = updateInfo.Version,
                    downloadUrl = updateInfo.DownloadUrl,
                    changes = updateInfo.Changes,
                    required = updateInfo.Required,
                    status = "ok"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] ERROR obteniendo versión");
                return StatusCode(500, new { status = "error", message = "Error interno del servidor" });
            }
        }

        [HttpGet("info")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public IActionResult Info()
        {
            try
            {
                _logger.LogDebug("[API] GET /api/launcher/info");

                return Ok(new
                {
                    serverName = "Launcher Phantom Server",
                    version = "1.0.0",
                    status = "online",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] ERROR en GET /api/launcher/info");
                return StatusCode(500, new { status = "error" });
            }
        }
    }
}
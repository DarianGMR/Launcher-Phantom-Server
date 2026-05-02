using Microsoft.AspNetCore.Mvc;

namespace LauncherPhantomServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LauncherController : ControllerBase
    {
        private readonly ILogger<LauncherController> _logger;
        private readonly IConfiguration _config;

        public LauncherController(ILogger<LauncherController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet("health")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public IActionResult Health()
        {
            try
            {
                return Ok(new
                {
                    status = "ok",
                    message = "Servidor funcionando correctamente",
                    timestamp = DateTime.UtcNow,
                    version = _config["App:Version"] ?? "1.0.0"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LauncherController] Error en Health check");
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
        public IActionResult Version()
        {
            try
            {
                return Ok(new
                {
                    version = _config["App:Version"] ?? "1.0.0",
                    releaseDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    status = "ok",
                    downloadUrl = _config["App:DownloadUrl"],
                    required = _config.GetValue<bool>("App:RequiredUpdate", false),
                    changes = new[]
                    {
                        "Optimización de base de datos",
                        "Caché de memoria implementado",
                        "Rate limiting agregado",
                        "Validación completa de entrada",
                        "Compresión de respuestas",
                        "Mejor manejo de errores"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LauncherController] Error obteniendo versión");
                return StatusCode(500, new { status = "error", message = "Error interno del servidor" });
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using LauncherPhantomServer.Models;
using LauncherPhantomServer.Services;

namespace LauncherPhantomServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [EnableRateLimiting("strict")]
        [ProducesResponseType(200, Type = typeof(AuthResponse))]
        [ProducesResponseType(400, Type = typeof(AuthResponse))]
        [ProducesResponseType(401, Type = typeof(AuthResponse))]
        [ProducesResponseType(500, Type = typeof(AuthResponse))]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse 
                    { 
                        Success = false, 
                        Error = "Datos de solicitud inválidos" 
                    });
                }

                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                _logger.LogInformation($"[API] POST /api/auth/login - Usuario: {request.Username} - IP: {clientIp}");

                var response = await _authService.LoginAsync(request, clientIp);

                if (!response.Success)
                {
                    _logger.LogInformation($"[API] Login fallido para: {request.Username}");
                }

                return response.Success ? Ok(response) : Unauthorized(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] Error en POST /api/auth/login");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Error = "Error interno del servidor"
                });
            }
        }

        [HttpPost("register")]
        [EnableRateLimiting("strict")]
        [ProducesResponseType(200, Type = typeof(AuthResponse))]
        [ProducesResponseType(400, Type = typeof(AuthResponse))]
        [ProducesResponseType(500, Type = typeof(AuthResponse))]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Error = "Datos de solicitud inválidos"
                    });
                }

                _logger.LogInformation($"[API] POST /api/auth/register - Usuario: {request.Username}");

                var response = await _authService.RegisterAsync(request);

                if (!response.Success)
                {
                    _logger.LogInformation($"[API] Registro fallido para: {request.Username}");
                }

                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] Error en POST /api/auth/register");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Error = "Error interno del servidor"
                });
            }
        }
    }
}
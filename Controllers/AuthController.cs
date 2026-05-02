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

        /// <summary>
        /// Login de usuario
        /// </summary>
        [HttpPost("login")]
        [EnableRateLimiting("strict")] // Rate limiting estricto
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
                _logger.LogInformation($"[AuthController] Login request para: {request.Username} desde {clientIp}");

                var response = await _authService.LoginAsync(request, clientIp);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return Unauthorized(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController] Error en Login");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Error = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Registro de nuevo usuario
        /// </summary>
        [HttpPost("register")]
        [EnableRateLimiting("strict")] // Rate limiting estricto
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

                _logger.LogInformation($"[AuthController] Register request para: {request.Username}");

                var response = await _authService.RegisterAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController] Error en Register");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Error = "Error interno del servidor"
                });
            }
        }
    }
}
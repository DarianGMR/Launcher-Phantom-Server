using Microsoft.AspNetCore.Mvc;
using LauncherPhantomServer.Models;
using LauncherPhantomServer.Services;
using System.Diagnostics;

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
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("[AuthController] Login request para usuario: {Username}", request.Username);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[AuthController] Estado de modelo inválido en login");
                    return BadRequest(new AuthResponse { Success = false, Error = "Datos inválidos" });
                }

                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                _logger.LogInformation("[AuthController] IP del cliente: {ClientIp}", clientIp);

                var response = await _authService.LoginAsync(request, clientIp);

                _logger.LogInformation("[AuthController] Respuesta de login - Éxito: {Success}", response.Success);

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
                _logger.LogError(ex, "[AuthController] Excepción en Login");
                return StatusCode(500, new AuthResponse 
                { 
                    Success = false, 
                    Error = $"Error del servidor: {ex.Message}" 
                });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("[AuthController] Register request para usuario: {Username}", request.Username);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[AuthController] Estado de modelo inválido en register");
                    return BadRequest(new AuthResponse { Success = false, Error = "Datos inválidos" });
                }

                var response = await _authService.RegisterAsync(request);

                _logger.LogInformation("[AuthController] Respuesta de register - Éxito: {Success}", response.Success);

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
                _logger.LogError(ex, "[AuthController] Excepción en Register");
                return StatusCode(500, new AuthResponse 
                { 
                    Success = false, 
                    Error = $"Error del servidor: {ex.Message}" 
                });
            }
        }
    }
}

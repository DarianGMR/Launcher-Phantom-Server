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
                _logger.LogInformation("[AuthController] Login request for user: {Username}", request.Username);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[AuthController] Invalid model state");
                    return BadRequest(new AuthResponse { Success = false, Error = "Datos inválidos" });
                }

                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                _logger.LogInformation("[AuthController] Client IP: {ClientIp}", clientIp);

                var response = await _authService.LoginAsync(request, clientIp);

                _logger.LogInformation("[AuthController] Login response success: {Success}", response.Success);

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
                _logger.LogError(ex, "[AuthController] Exception in Login");
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
                _logger.LogInformation("[AuthController] Register request for user: {Username}", request.Username);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[AuthController] Invalid model state");
                    return BadRequest(new AuthResponse { Success = false, Error = "Datos inválidos" });
                }

                var response = await _authService.RegisterAsync(request);

                _logger.LogInformation("[AuthController] Register response success: {Success}", response.Success);

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
                _logger.LogError(ex, "[AuthController] Exception in Register");
                return StatusCode(500, new AuthResponse 
                { 
                    Success = false, 
                    Error = $"Error del servidor: {ex.Message}" 
                });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LauncherPhantomServer.Models;
using LauncherPhantomServer.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace LauncherPhantomServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("moderate")]
    public class AdminController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly BanService _banService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserService userService,
            BanService banService,
            ILogger<AdminController> logger)
        {
            _userService = userService;
            _banService = banService;
            _logger = logger;
        }

        [HttpGet("users")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                _logger.LogInformation("[API] GET /api/admin/users");
                
                var users = await _userService.GetAllUsersAsync();
                
                var result = users.Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.CreatedAt,
                    u.LastLogin,
                    u.IsActive,
                    u.LastIp,
                    BanCount = u.Bans.Count
                }).OrderByDescending(u => u.CreatedAt).ToList();

                _logger.LogInformation($"[API] Obtenidos {result.Count} usuarios");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] ERROR en GET /api/admin/users");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("users/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                _logger.LogInformation($"[API] GET /api/admin/users/{id}");
                
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"[API] Usuario no encontrado: {id}");
                    return NotFound(new { error = "Usuario no encontrado" });
                }

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.CreatedAt,
                    user.LastLogin,
                    user.IsActive,
                    user.LastIp,
                    Bans = user.Bans.Select(b => new
                    {
                        b.Id,
                        b.Reason,
                        b.BannedAt,
                        b.ExpiresAt,
                        b.IsPermanent
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[API] ERROR en GET /api/admin/users/{id}");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("bans")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetActiveBans()
        {
            try
            {
                _logger.LogInformation("[API] GET /api/admin/bans");
                
                var bans = await _banService.GetActiveBansAsync();

                var result = bans.Select(b => new
                {
                    b.Id,
                    b.UserId,
                    User = b.User == null ? null : new { b.User.Id, b.User.Username, b.User.Email },
                    b.IpAddress,
                    b.Reason,
                    b.BannedAt,
                    b.ExpiresAt,
                    b.IsPermanent
                }).OrderByDescending(b => b.BannedAt).ToList();

                _logger.LogInformation($"[API] Obtenidos {result.Count} bans activos");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] ERROR en GET /api/admin/bans");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpPost("ban")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> BanUser([FromBody] BanRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[API] POST /api/admin/ban - Validación fallida");
                    return BadRequest(ModelState);
                }

                if (request.UserId <= 0 || string.IsNullOrWhiteSpace(request.Reason))
                {
                    _logger.LogWarning("[API] POST /api/admin/ban - Datos inválidos");
                    return BadRequest(new { error = "Datos de ban inválidos" });
                }

                _logger.LogInformation($"[API] POST /api/admin/ban - Usuario: {request.UserId} - Razón: {request.Reason}");

                var user = await _userService.GetUserByIdAsync(request.UserId);
                if (user == null)
                {
                    _logger.LogWarning($"[API] Usuario no encontrado para ban: {request.UserId}");
                    return NotFound(new { error = "Usuario no encontrado" });
                }

                var success = await _banService.BanUserAsync(
                    request.UserId,
                    user.LastIp ?? "unknown",
                    request.Reason,
                    request.DurationHours,
                    request.AdminId
                );

                if (!success)
                {
                    _logger.LogError($"[API] Error al banear usuario: {request.UserId}");
                    return BadRequest(new { error = "Error al banear usuario" });
                }

                _logger.LogWarning($"[API] Usuario baneado exitosamente: {request.UserId}");

                return Ok(new { success = true, message = "Usuario baneado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] ERROR en POST /api/admin/ban");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpDelete("unban/{banId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UnbanUser(int banId)
        {
            try
            {
                _logger.LogInformation($"[API] DELETE /api/admin/unban/{banId}");
                
                var success = await _banService.UnbanUserAsync(banId);
                if (!success)
                {
                    _logger.LogWarning($"[API] Ban no encontrado: {banId}");
                    return NotFound(new { error = "Ban no encontrado" });
                }

                _logger.LogInformation($"[API] Ban eliminado exitosamente: {banId}");

                return Ok(new { success = true, message = "Usuario desbaneado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[API] ERROR en DELETE /api/admin/unban/{banId}");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpPost("deactivate/{userId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            try
            {
                _logger.LogInformation($"[API] POST /api/admin/deactivate/{userId}");
                
                var success = await _userService.DeactivateUserAsync(userId);
                if (!success)
                {
                    _logger.LogWarning($"[API] Usuario no encontrado: {userId}");
                    return NotFound(new { error = "Usuario no encontrado" });
                }

                _logger.LogWarning($"[API] Usuario desactivado: {userId}");

                return Ok(new { success = true, message = "Usuario desactivado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[API] ERROR en POST /api/admin/deactivate/{userId}");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpDelete("user/{userId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                _logger.LogInformation($"[API] DELETE /api/admin/user/{userId}");
                
                var success = await _userService.DeleteUserAsync(userId);
                if (!success)
                {
                    _logger.LogWarning($"[API] Usuario no encontrado: {userId}");
                    return NotFound(new { error = "Usuario no encontrado" });
                }

                _logger.LogWarning($"[API] Usuario eliminado: {userId}");

                return Ok(new { success = true, message = "Usuario eliminado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[API] ERROR en DELETE /api/admin/user/{userId}");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }
    }

    public class BanRequest
    {
        public int UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int DurationHours { get; set; }
        public int AdminId { get; set; }
    }
}
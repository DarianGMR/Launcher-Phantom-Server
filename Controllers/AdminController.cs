using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LauncherPhantomServer.Models;
using LauncherPhantomServer.Services;

namespace LauncherPhantomServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly BanService _banService;

        public AdminController(UserService userService, BanService banService)
        {
            _userService = userService;
            _banService = banService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.CreatedAt,
                u.LastLogin,
                u.IsActive,
                u.LastIp
            }));
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            
            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.CreatedAt,
                user.LastLogin,
                user.IsActive,
                user.LastIp,
                Bans = user.Bans.Select(b => new { b.Id, b.Reason, b.ExpiresAt, b.IsPermanent })
            });
        }

        [HttpGet("bans")]
        public async Task<IActionResult> GetActiveBans()
        {
            var bans = await _banService.GetActiveBansAsync();
            return Ok(bans.Select(b => new
            {
                b.Id,
                b.UserId,
                User = b.User == null ? null : new { b.User.Id, b.User.Username, b.User.Email },
                b.IpAddress,
                b.Reason,
                b.BannedAt,
                b.ExpiresAt,
                b.IsPermanent
            }));
        }

        [HttpPost("ban")]
        public async Task<IActionResult> BanUser([FromBody] BanRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user == null) return NotFound("Usuario no encontrado");

            var success = await _banService.BanUserAsync(
                request.UserId,
                user.LastIp ?? "unknown",
                request.Reason,
                request.DurationHours,
                request.AdminId
            );

            if (!success) return BadRequest("Error al banear el usuario");

            return Ok(new { success = true, message = "Usuario baneado correctamente" });
        }

        [HttpDelete("unban/{banId}")]
        public async Task<IActionResult> UnbanUser(int banId)
        {
            var success = await _banService.UnbanUserAsync(banId);
            if (!success) return NotFound("Ban no encontrado");

            return Ok(new { success = true, message = "Usuario desbaneado correctamente" });
        }

        [HttpPost("deactivate/{userId}")]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            var success = await _userService.DeactivateUserAsync(userId);
            if (!success) return NotFound();

            return Ok(new { success = true, message = "Usuario desactivado" });
        }

        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var success = await _userService.DeleteUserAsync(userId);
            if (!success) return NotFound();

            return Ok(new { success = true, message = "Usuario eliminado" });
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
using LauncherPhantomServer.Data;
using LauncherPhantomServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LauncherPhantomServer.Services
{
    public class BanService
    {
        private readonly DatabaseContext _context;
        private readonly CacheService _cacheService;
        private readonly ILogger<BanService> _logger;

        public BanService(DatabaseContext context, CacheService cacheService, ILogger<BanService> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Banea un usuario
        /// </summary>
        public async Task<bool> BanUserAsync(int userId, string ipAddress, string reason, int durationHours, int adminId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(reason))
                {
                    _logger.LogWarning("[BanService] Datos de ban incompletos");
                    return false;
                }

                var ban = new Ban
                {
                    UserId = userId,
                    IpAddress = ipAddress.Trim(),
                    Reason = reason.Trim(),
                    BannedAt = DateTime.UtcNow,
                    ExpiresAt = durationHours == 0 ? DateTime.MaxValue : DateTime.UtcNow.AddHours(durationHours),
                    IsPermanent = durationHours == 0,
                    BannedByAdminId = adminId
                };

                _context.Bans.Add(ban);

                // ✅ Desactivar usuario en la misma operación
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsActive = false;
                    _context.Users.Update(user);
                    _cacheService.Remove(_cacheService.GetUserCacheKey(userId));
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[BanService] ✅ Usuario {userId} baneado por: {reason}");
                
                // ✅ Limpiar caché de bans
                _cacheService.Remove(_cacheService.GetActiveBansCacheKey());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BanService] Error al banear usuario");
                return false;
            }
        }

        /// <summary>
        /// Desbanea un usuario
        /// </summary>
        public async Task<bool> UnbanUserAsync(int banId)
        {
            try
            {
                var ban = await _context.Bans.FindAsync(banId);
                if (ban == null)
                {
                    _logger.LogWarning($"[BanService] Ban no encontrado: {banId}");
                    return false;
                }

                _context.Bans.Remove(ban);

                // ✅ Reactivar usuario si no tiene otros bans activos
                var user = await _context.Users.FindAsync(ban.UserId);
                if (user != null)
                {
                    var otherActiveBans = await _context.Bans
                        .AsNoTracking()
                        .AnyAsync(b => b.UserId == user.Id && (b.IsPermanent || b.ExpiresAt > DateTime.UtcNow));

                    if (!otherActiveBans)
                    {
                        user.IsActive = true;
                        _context.Users.Update(user);
                        _cacheService.Remove(_cacheService.GetUserCacheKey(user.Id));
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[BanService] ✅ Ban removido: {banId}");
                _cacheService.Remove(_cacheService.GetActiveBansCacheKey());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BanService] Error al desbanear usuario");
                return false;
            }
        }

        /// <summary>
        /// Obtiene bans activos (con caché)
        /// </summary>
        public async Task<List<Ban>> GetActiveBansAsync()
        {
            try
            {
                var cacheKey = _cacheService.GetActiveBansCacheKey();
                var cachedBans = _cacheService.Get<List<Ban>>(cacheKey);

                if (cachedBans != null)
                {
                    return cachedBans;
                }

                // ✅ Índice compuesto mejora esta consulta significativamente
                var activeBans = await _context.Bans
                    .Where(b => b.IsPermanent || b.ExpiresAt > DateTime.UtcNow)
                    .Include(b => b.User)
                    .AsNoTracking()
                    .ToListAsync();

                // ✅ Cachear por 1 minuto
                _cacheService.Set(cacheKey, activeBans, TimeSpan.FromMinutes(1));

                return activeBans;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BanService] Error obteniendo bans activos");
                return new List<Ban>();
            }
        }

        /// <summary>
        /// Obtiene bans de un usuario específico
        /// </summary>
        public async Task<List<Ban>> GetUserBansAsync(int userId)
        {
            try
            {
                return await _context.Bans
                    .Where(b => b.UserId == userId)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[BanService] Error obteniendo bans para usuario {userId}");
                return new List<Ban>();
            }
        }

        /// <summary>
        /// Obtiene ban por IP (usado por middleware)
        /// </summary>
        public async Task<Ban?> GetBanByIpAsync(string ipAddress)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ipAddress))
                    return null;

                // ✅ Índice en IpAddress mejora esta búsqueda
                return await _context.Bans
                    .Where(b => b.IpAddress == ipAddress.Trim() && (b.IsPermanent || b.ExpiresAt > DateTime.UtcNow))
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[BanService] Error verificando ban por IP");
                return null;
            }
        }

        /// <summary>
        /// Limpia bans expirados (ejecutar periódicamente)
        /// </summary>
        public async Task<int> CleanupExpiredBansAsync()
        {
            try
            {
                var expiredBans = await _context.Bans
                    .Where(b => !b.IsPermanent && b.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync();

                if (expiredBans.Count == 0)
                    return 0;

                _context.Bans.RemoveRange(expiredBans);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[BanService] ✅ Se limpiaron {expiredBans.Count} bans expirados");
                _cacheService.Remove(_cacheService.GetActiveBansCacheKey());

                return expiredBans.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BanService] Error limpiando bans expirados");
                return 0;
            }
        }
    }
}
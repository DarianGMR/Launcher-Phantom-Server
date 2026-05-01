using LauncherPhantomServer.Data;
using LauncherPhantomServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LauncherPhantomServer.Services
{
    public class UserService
    {
        private readonly DatabaseContext _context;
        private readonly CacheService _cacheService;
        private readonly ILogger<UserService> _logger;

        public UserService(DatabaseContext context, CacheService cacheService, ILogger<UserService> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los usuarios (con caché)
        /// </summary>
        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var cacheKey = _cacheService.GetUserListCacheKey();
                var cachedUsers = _cacheService.Get<List<User>>(cacheKey);

                if (cachedUsers != null)
                {
                    return cachedUsers;
                }

                // ✅ Usar proyección para obtener solo datos necesarios
                var users = await _context.Users
                    .Include(u => u.Bans)
                    .AsNoTracking()
                    .ToListAsync();

                // ✅ Cachear por 5 minutos
                _cacheService.Set(cacheKey, users, TimeSpan.FromMinutes(5));

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserService] Error obteniendo usuarios");
                return new List<User>();
            }
        }

        /// <summary>
        /// Obtiene usuario por ID (con caché)
        /// </summary>
        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                var cacheKey = _cacheService.GetUserCacheKey(id);
                var cachedUser = _cacheService.Get<User>(cacheKey);

                if (cachedUser != null)
                {
                    return cachedUser;
                }

                var user = await _context.Users
                    .Include(u => u.Bans)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user != null)
                {
                    _cacheService.Set(cacheKey, user, TimeSpan.FromHours(24));
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[UserService] Error obteniendo usuario {id}");
                return null;
            }
        }

        /// <summary>
        /// Actualiza un usuario
        /// </summary>
        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                if (user == null || user.Id == 0)
                {
                    _logger.LogWarning("[UserService] Intento de actualizar usuario inválido");
                    return false;
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // ✅ Limpiar caché
                _cacheService.Remove(_cacheService.GetUserCacheKey(user.Id));
                _cacheService.Remove(_cacheService.GetUserListCacheKey());

                _logger.LogInformation($"[UserService] ✅ Usuario {user.Id} actualizado");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserService] Error actualizando usuario");
                return false;
            }
        }

        /// <summary>
        /// Elimina un usuario (hard delete)
        /// </summary>
        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"[UserService] Usuario no encontrado: {id}");
                    return false;
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _cacheService.Remove(_cacheService.GetUserCacheKey(id));
                _cacheService.Remove(_cacheService.GetUserListCacheKey());

                _logger.LogInformation($"[UserService] ✅ Usuario {id} eliminado");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserService] Error eliminando usuario");
                return false;
            }
        }

        /// <summary>
        /// Desactiva un usuario (soft delete)
        /// </summary>
        public async Task<bool> DeactivateUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"[UserService] Usuario no encontrado: {id}");
                    return false;
                }

                user.IsActive = false;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _cacheService.Remove(_cacheService.GetUserCacheKey(id));
                _cacheService.Remove(_cacheService.GetUserListCacheKey());

                _logger.LogInformation($"[UserService] ✅ Usuario {id} desactivado");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserService] Error desactivando usuario");
                return false;
            }
        }

        /// <summary>
        /// Obtiene usuarios activos
        /// </summary>
        public async Task<List<User>> GetActiveUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.IsActive)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserService] Error obteniendo usuarios activos");
                return new List<User>();
            }
        }
    }
}
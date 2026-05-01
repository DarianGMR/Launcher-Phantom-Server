using Microsoft.Extensions.Caching.Memory;
using System;

namespace LauncherPhantomServer.Services
{
    /// <summary>
    /// Servicio centralizado de caché para mejorar el rendimiento
    /// </summary>
    public class CacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;
        private const string USER_CACHE_KEY = "user_{0}";
        private const string USERS_LIST_CACHE_KEY = "users_list";
        private const string BAN_CACHE_KEY = "ban_{0}";
        private const string ACTIVE_BANS_CACHE_KEY = "active_bans";
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public T? Get<T>(string key)
        {
            try
            {
                return _cache.TryGetValue(key, out T? value) ? value : default;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error obteniendo del caché: {ex.Message}");
                return default;
            }
        }

        public void Set<T>(string key, T value, TimeSpan? duration = null)
        {
            try
            {
                _cache.Set(key, value, duration ?? DefaultCacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error guardando en caché: {ex.Message}");
            }
        }

        public void Remove(string key)
        {
            try
            {
                _cache.Remove(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error removiendo del caché: {ex.Message}");
            }
        }

        public void RemovePattern(string pattern)
        {
            // Para patrones, es mejor limpiar manualmente
            Remove(pattern);
        }

        public string GetUserCacheKey(int userId) => string.Format(USER_CACHE_KEY, userId);
        public string GetBanCacheKey(int banId) => string.Format(BAN_CACHE_KEY, banId);
        public string GetUserListCacheKey() => USERS_LIST_CACHE_KEY;
        public string GetActiveBansCacheKey() => ACTIVE_BANS_CACHE_KEY;
    }
}
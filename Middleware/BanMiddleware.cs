using LauncherPhantomServer.Services;

namespace LauncherPhantomServer.Middleware
{
    public class BanMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BanMiddleware> _logger;

        // Rutas que no requieren validación de ban
        private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/launcher/health",
            "/api/launcher/version",
            "/health",
            "/swagger",
            "/swagger/ui",
            "/swagger/v1"
        };

        public BanMiddleware(RequestDelegate next, ILogger<BanMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, BanService banService)
        {
            var clientIp = ExtractClientIp(context);
            var path = context.Request.Path.Value ?? "";

            _logger.LogDebug($"[BanMiddleware] Solicitud de {clientIp} a {path}");

            // Skip validation para rutas excluidas
            if (IsExcludedPath(path))
            {
                await _next(context);
                return;
            }

            try
            {
                // Validar ban por IP
                var ban = await banService.GetBanByIpAsync(clientIp);
                if (ban != null)
                {
                    _logger.LogWarning($"[BanMiddleware] IP baneada detectada: {clientIp}");
                    
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    
                    var remainingTime = ban.IsPermanent 
                        ? "permanentemente" 
                        : $"hasta {ban.ExpiresAt:g}";

                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        error = $"IP baneada {remainingTime}",
                        reason = ban.Reason,
                        expiresAt = ban.ExpiresAt
                    });

                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BanMiddleware] Error validando ban");
                // Continuar en lugar de fallar
            }

            await _next(context);
        }

        private string ExtractClientIp(HttpContext context)
        {
            // Manejar X-Forwarded-For para proxies
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ip = forwardedFor.ToString().Split(',').First().Trim();
                return ip;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool IsExcludedPath(string path)
        {
            return ExcludedPaths.Any(excluded => 
                path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
        }
    }
}
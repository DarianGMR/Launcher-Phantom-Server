using LauncherPhantomServer.Services;
using System.Diagnostics;

namespace LauncherPhantomServer.Middleware
{
    public class BanMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BanMiddleware> _logger;

        public BanMiddleware(RequestDelegate next, ILogger<BanMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, BanService banService)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            _logger.LogInformation("[BanMiddleware] Solicitud de: {ClientIp} a {Path}", clientIp, context.Request.Path);

            // No aplicar ban middleware a estos endpoints
            if (context.Request.Path.StartsWithSegments("/api/auth/login") || 
                context.Request.Path.StartsWithSegments("/api/auth/register") ||
                context.Request.Path.StartsWithSegments("/api/launcher/health") ||
                context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            var ban = await banService.GetBanByIpAsync(clientIp);
            if (ban != null)
            {
                _logger.LogWarning("[BanMiddleware] IP baneada detectada: {ClientIp}. Razón: {Reason}", clientIp, ban.Reason);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    error = $"IP baneada. Razón: {ban.Reason}. Expira: {ban.ExpiresAt:g}"
                });
                return;
            }

            await _next(context);
        }
    }
}

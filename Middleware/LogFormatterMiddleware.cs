using System.Diagnostics;

namespace LauncherPhantomServer.Middleware
{
    /// <summary>
    /// Middleware para simplificar y formatear los logs de las API requests
    /// Intercepta las solicitudes y registra solo la información esencial
    /// </summary>
    public class LogFormatterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogFormatterMiddleware> _logger;

        public LogFormatterMiddleware(RequestDelegate next, ILogger<LogFormatterMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "";
            var clientIp = ExtractClientIp(context);

            try
            {
                await _next(context);
                stopwatch.Stop();

                // Log simplificado solo para requests importantes
                if (!IsExcludedPath(path))
                {
                    var statusCode = context.Response.StatusCode;
                    var level = statusCode >= 500 ? LogLevel.Error :
                               statusCode >= 400 ? LogLevel.Warning :
                               LogLevel.Information;

                    var statusEmoji = statusCode switch
                    {
                        >= 500 => "❌",
                        >= 400 => "⚠️",
                        >= 300 => "↪️",
                        _ => "✅"
                    };

                    _logger.Log(level, $"{statusEmoji} [{method}] {path} - {statusCode} ({stopwatch.ElapsedMilliseconds}ms)");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"❌ [{method}] {path} - EXCEPCIÓN ({stopwatch.ElapsedMilliseconds}ms)");
                throw;
            }
        }

        private string ExtractClientIp(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.ToString().Split(',').First().Trim();
            }
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool IsExcludedPath(string path)
        {
            var excluded = new[] { "/swagger", "/health", "/metrics", "/_" };
            return excluded.Any(ex => path.StartsWith(ex, StringComparison.OrdinalIgnoreCase));
        }
    }
}
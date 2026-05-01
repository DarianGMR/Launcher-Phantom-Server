using LauncherPhantomServer.Services;

namespace LauncherPhantomServer.Middleware
{
    public class BanMiddleware
    {
        private readonly RequestDelegate _next;

        public BanMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, BanService banService)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var ban = await banService.GetBanByIpAsync(clientIp);
            if (ban != null)
            {
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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LauncherPhantomServer.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwtService> _logger;
        private readonly SymmetricSecurityKey _key;

        public JwtService(IConfiguration config, ILogger<JwtService> logger)
        {
            _config = config;
            _logger = logger;

            var keyString = config["Jwt:Key"] ?? "your-secret-key-change-in-production-minimum-32-characters";
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        }

        public string GenerateToken(int userId, string username, string email)
        {
            try
            {
                var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Email, email),
                    new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                };

                var jwtExpiryMinutes = _config.GetValue<int>("Jwt:ExpiryMinutes", 1440); // 24 horas por defecto
                var expires = DateTime.UtcNow.AddMinutes(jwtExpiryMinutes);

                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"] ?? "LauncherPhantomServer",
                    audience: _config["Jwt:Audience"] ?? "LauncherPhantomClient",
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                _logger.LogInformation($"[JwtService] ✅ Token generado para usuario: {username}");

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[JwtService] Error generando token");
                throw;
            }
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _key,
                    ValidateIssuer = true,
                    ValidIssuer = _config["Jwt:Issuer"] ?? "LauncherPhantomServer",
                    ValidateAudience = true,
                    ValidAudience = _config["Jwt:Audience"] ?? "LauncherPhantomClient",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return validatedToken is JwtSecurityToken;
            }
            catch
            {
                return false;
            }
        }
    }
}
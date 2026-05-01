using LauncherPhantomServer.Data;
using LauncherPhantomServer.Models;
using System.Diagnostics;

namespace LauncherPhantomServer.Services
{
    public class AuthService
    {
        private readonly DatabaseContext _context;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(DatabaseContext context, JwtService jwtService, ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("[AuthService] Register attempt for username: {Username}", request.Username);

                if (string.IsNullOrWhiteSpace(request.Username) || 
                    string.IsNullOrWhiteSpace(request.Email) || 
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("[AuthService] Incomplete data in register request");
                    return new AuthResponse { Success = false, Error = "Datos incompletos" };
                }

                var existingUser = _context.Users.FirstOrDefault(u => u.Username == request.Username);
                if (existingUser != null)
                {
                    _logger.LogWarning("[AuthService] Username already exists: {Username}", request.Username);
                    return new AuthResponse { Success = false, Error = "El usuario ya existe" };
                }

                var existingEmail = _context.Users.FirstOrDefault(u => u.Email == request.Email);
                if (existingEmail != null)
                {
                    _logger.LogWarning("[AuthService] Email already exists: {Email}", request.Email);
                    return new AuthResponse { Success = false, Error = "El email ya está registrado" };
                }

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[AuthService] User registered successfully: {Username}", request.Username);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Cuenta creada exitosamente",
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthService] Error in RegisterAsync");
                return new AuthResponse { Success = false, Error = $"Error: {ex.Message}" };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, string clientIp)
        {
            try
            {
                _logger.LogInformation("[AuthService] Login attempt for username: {Username}", request.Username);

                if (string.IsNullOrWhiteSpace(request.Username) || 
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("[AuthService] Empty credentials in login request");
                    return new AuthResponse { Success = false, Error = "Usuario o contraseña incorrectos" };
                }

                var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);
                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("[AuthService] Invalid credentials for user: {Username}", request.Username);
                    return new AuthResponse { Success = false, Error = "Usuario o contraseña incorrectos" };
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("[AuthService] Inactive account attempt: {Username}", request.Username);
                    return new AuthResponse { Success = false, Error = "La cuenta ha sido desactivada" };
                }

                // Check if user is banned
                var activeBan = _context.Bans
                    .Where(b => b.UserId == user.Id && (b.IsPermanent || b.ExpiresAt > DateTime.UtcNow))
                    .FirstOrDefault();

                if (activeBan != null)
                {
                    var remainingTime = activeBan.IsPermanent ? "permanentemente" : $"hasta {activeBan.ExpiresAt:g}";
                    _logger.LogWarning("[AuthService] Banned user login attempt: {Username}", request.Username);
                    return new AuthResponse 
                    { 
                        Success = false, 
                        Error = $"Cuenta baneada {remainingTime}. Razón: {activeBan.Reason}" 
                    };
                }

                user.LastLogin = DateTime.UtcNow;
                user.LastIp = clientIp;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                var token = _jwtService.GenerateToken(user.Id, user.Username, user.Email);

                _logger.LogInformation("[AuthService] Login successful for user: {Username}", request.Username);

                return new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthService] Error in LoginAsync");
                return new AuthResponse { Success = false, Error = $"Error: {ex.Message}" };
            }
        }
    }
}
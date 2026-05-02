using LauncherPhantomServer.Data;
using LauncherPhantomServer.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LauncherPhantomServer.Services
{
    public class AuthService
    {
        private readonly DatabaseContext _context;
        private readonly JwtService _jwtService;
        private readonly CacheService _cacheService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            DatabaseContext context,
            JwtService jwtService,
            CacheService cacheService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Registra un nuevo usuario con validaciones completas
        /// </summary>
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                //  Validación de entrada
                var validationResult = ValidateRegisterRequest(request);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning($"[AuthService] Validación fallida en registro: {validationResult.ErrorMessage}");
                    return new AuthResponse { Success = false, Error = validationResult.ErrorMessage };
                }

                _logger.LogInformation($"[AuthService] Intento de registro para usuario: {request.Username}");

                //  Validar username único (usar AsNoTracking para mejor rendimiento)
                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == request.Username.ToLower());

                if (existingUser != null)
                {
                    _logger.LogWarning($"[AuthService] Username ya existe: {request.Username}");
                    return new AuthResponse { Success = false, Error = "El usuario ya existe" };
                }

                //  Validar email único
                var existingEmail = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (existingEmail != null)
                {
                    _logger.LogWarning($"[AuthService] Email ya registrado: {request.Email}");
                    return new AuthResponse { Success = false, Error = "El email ya está registrado" };
                }

                //  Crear usuario con contraseña hasheada
                var user = new User
                {
                    Username = request.Username.ToLower(),
                    Email = request.Email.ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, BCrypt.Net.BCrypt.GenerateSalt(12)),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[AuthService] Usuario registrado exitosamente: {request.Username}");

                //  Limpiar caché de lista de usuarios
                _cacheService.Remove(_cacheService.GetUserListCacheKey());

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
                _logger.LogError(ex, "[AuthService] Error en RegisterAsync");
                return new AuthResponse { Success = false, Error = "Error al registrar usuario" };
            }
        }

        /// <summary>
        /// Login con verificación de bans activos
        /// </summary>
        public async Task<AuthResponse> LoginAsync(LoginRequest request, string clientIp)
        {
            try
            {
                //  Validación de entrada
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("[AuthService] Credenciales vacías en login");
                    return new AuthResponse { Success = false, Error = "Usuario o contraseña incorrectos" };
                }

                _logger.LogInformation($"[AuthService] Intento de login para: {request.Username} desde IP: {clientIp}");

                //  Buscar usuario (con Include para obtener bans en una sola consulta)
                var user = await _context.Users
                    .Include(u => u.Bans)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == request.Username.ToLower());

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"[AuthService] Credenciales inválidas para: {request.Username}");
                    return new AuthResponse { Success = false, Error = "Usuario o contraseña incorrectos" };
                }

                //  Verificar si cuenta está activa
                if (!user.IsActive)
                {
                    _logger.LogWarning($"[AuthService] Intento de login en cuenta desactivada: {request.Username}");
                    return new AuthResponse { Success = false, Error = "La cuenta ha sido desactivada" };
                }

                //  Verificar bans activos
                var activeBan = user.Bans
                    .FirstOrDefault(b => b.IsPermanent || b.ExpiresAt > DateTime.UtcNow);

                if (activeBan != null)
                {
                    var remainingTime = activeBan.IsPermanent 
                        ? "permanentemente" 
                        : $"hasta {activeBan.ExpiresAt:g}";
                    
                    _logger.LogWarning($"[AuthService] Usuario baneado intenta login: {request.Username}");
                    return new AuthResponse
                    {
                        Success = false,
                        Error = $"Cuenta baneada {remainingTime}. Razón: {activeBan.Reason}"
                    };
                }

                //  Actualizar último login y IP (usar tracking)
                user.LastLogin = DateTime.UtcNow;
                user.LastIp = clientIp;
                
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                //  Generar token JWT
                var token = _jwtService.GenerateToken(user.Id, user.Username, user.Email);

                _logger.LogInformation($"[AuthService] Login exitoso para: {request.Username}");

                //  Cachear usuario durante 24 horas
                _cacheService.Set(_cacheService.GetUserCacheKey(user.Id), user, TimeSpan.FromHours(24));

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
                _logger.LogError(ex, "[AuthService] Error en LoginAsync");
                return new AuthResponse { Success = false, Error = "Error al procesar login" };
            }
        }

        /// <summary>
        /// Valida los datos de registro
        /// </summary>
        private ValidationResult ValidateRegisterRequest(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return ValidationResult.Fail("El usuario es requerido");

            if (request.Username.Length < 3 || request.Username.Length > 50)
                return ValidationResult.Fail("El usuario debe tener entre 3 y 50 caracteres");

            if (!request.Username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                return ValidationResult.Fail("El usuario solo puede contener letras, números, guiones y guiones bajos");

            if (string.IsNullOrWhiteSpace(request.Email))
                return ValidationResult.Fail("El email es requerido");

            if (!IsValidEmail(request.Email))
                return ValidationResult.Fail("El email no es válido");

            if (string.IsNullOrWhiteSpace(request.Password))
                return ValidationResult.Fail("La contraseña es requerida");

            if (request.Password.Length < 8)
                return ValidationResult.Fail("La contraseña debe tener al menos 8 caracteres");

            if (!HasComplexPassword(request.Password))
                return ValidationResult.Fail("La contraseña debe contener mayúsculas, minúsculas, números y caracteres especiales");

            return ValidationResult.Success;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email && email.Length <= 255;
            }
            catch
            {
                return false;
            }
        }

        private bool HasComplexPassword(string password)
        {
            return password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(c => !char.IsLetterOrDigit(c));
        }

        private class ValidationResult
        {
            public bool IsValid { get; private set; }
            public string ErrorMessage { get; private set; } = string.Empty;

            public static ValidationResult Success => new() { IsValid = true };
            public static ValidationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
        }
    }
}
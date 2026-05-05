using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LauncherPhantomServer.Data;
using LauncherPhantomServer.Services;
using LauncherPhantomServer.Middleware;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Optimizaciones de rendimiento
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
    options.EnableForHttps = true;
});

// Add services
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values.SelectMany(v => v.Errors);
            return new BadRequestObjectResult(new 
            { 
                success = false, 
                errors = errors.Select(e => e.ErrorMessage).ToList() 
            });
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPhantomClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Type", "X-Total-Count");
    });
});

// Database con pooling optimizado
builder.Services.AddDbContext<DatabaseContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=launcher.db";
    options.UseSqlite(connectionString)
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// Services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<BanService>();
builder.Services.AddScoped<UpdateService>();

// Caching distribuido en memoria
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CacheService>();

// Logging mejorado - SOLO LOGS IMPORTANTES
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("JWT Key must be configured and at least 32 characters long");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LauncherPhantomServer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LauncherPhantomClient";
var jwtExpiryMinutes = builder.Configuration.GetValue<int>("Jwt:ExpiryMinutes", 1440);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Política estricta (5 requests por minuto)
    options.AddPolicy("strict", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    
    // Política moderada (100 requests por minuto)
    options.AddPolicy("moderate", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
});

var app = builder.Build();

// Inicializar base de datos y crear carpeta Update
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Database.EnsureCreated();
        Console.WriteLine("[DATABASE] Base de datos inicializada correctamente");
        
        // Crear carpeta Update y archivo update.json
        var updateService = scope.ServiceProvider.GetRequiredService<UpdateService>();
        updateService.InitializeUpdateFolder();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Error al inicializar BD: {ex.Message}");
    throw;
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Usar compresión de respuestas
app.UseResponseCompression();

app.UseRouting();
app.UseCors("AllowPhantomClient");

// Aplicar rate limiting
app.UseRateLimiter();

Console.WriteLine("[INIT] Aplicando middleware de seguridad y autenticación...");

// Ban Middleware
app.UseMiddleware<BanMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configurar StaticFiles para servir desde la carpeta Update en la raíz
var updatePath = Path.Combine(Directory.GetCurrentDirectory(), "Update");
if (!Directory.Exists(updatePath))
{
    Directory.CreateDirectory(updatePath);
    Console.WriteLine($"[STATIC] Carpeta Update creada: {updatePath}");
}

var staticFileOptions = new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(updatePath),
    RequestPath = "/update"
};
app.UseStaticFiles(staticFileOptions);

// También servir archivos desde wwwroot (por defecto)
app.UseStaticFiles();

// Configuration
var port = builder.Configuration.GetValue<int>("Server:Port", 5000);
var host = builder.Configuration["Server:Host"] ?? "0.0.0.0";
var urls = $"http://{host}:{port}";

app.Urls.Clear();
app.Urls.Add(urls);

// Banner de inicio
Console.WriteLine("\n");
Console.WriteLine("╔════════════════════════════════════════════════════╗");
Console.WriteLine("║        LAUNCHER PHANTOM SERVER - OPTIMIZADO        ║");
Console.WriteLine("╠════════════════════════════════════════════════════╣");
Console.WriteLine($"║  URL: {urls.PadRight(45)}║");
Console.WriteLine($"║  Local: http://0.0.0.0:{port}         {new string(' ', Math.Max(0, 5 - port.ToString().Length))}              ║");
Console.WriteLine($"║  Health: http://0.0.0.0:{port}/api/launcher/health   ║");
Console.WriteLine($"║  Swagger: http://localhost:{port}/swagger            ║");
Console.WriteLine("║  Servidor iniciado correctamente                   ║");
Console.WriteLine("╚════════════════════════════════════════════════════╝\n");

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Error crítico: {ex.Message}");
    throw;
}
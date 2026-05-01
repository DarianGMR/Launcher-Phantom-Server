using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LauncherPhantomServer.Data;
using LauncherPhantomServer.Services;
using LauncherPhantomServer.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Database
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlite("Data Source=launcher.db"));

// Services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<BanService>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-secret-key-change-in-production-minimum-32-characters-long-key-here";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LauncherPhantomServer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LauncherPhantomClient";

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
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Create database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    db.Database.EnsureCreated();
    Console.WriteLine("[DATABASE] Base de datos inicializada correctamente");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    Console.WriteLine("[SWAGGER] Swagger disponible en /swagger");
}

app.UseRouting();
app.UseCors("AllowAll");

Console.WriteLine("[MIDDLEWARE] Aplicando middleware de bans y autenticación...");

// Ban Middleware
app.UseMiddleware<BanMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseStaticFiles();

// IMPORTANTE: En Development/Debug, usar IIS Express (puerto 5000) o Kestrel con --urls
var port = 5000;
var host = "0.0.0.0";
var urls = $"http://{host}:{port}";

app.Urls.Clear();
app.Urls.Add(urls);

Console.WriteLine($"[SERVER] ========================================");
Console.WriteLine($"[SERVER] Iniciando servidor en: {urls}");
Console.WriteLine($"[SERVER] URL Accesible: http://localhost:{port}");
Console.WriteLine($"[SERVER] Health Check: http://localhost:{port}/api/launcher/health");
Console.WriteLine($"[SERVER] Swagger: http://localhost:{port}/swagger");
Console.WriteLine($"[SERVER] ========================================");
Console.WriteLine("[SERVER] Presiona Ctrl+C para detener");

app.Run();

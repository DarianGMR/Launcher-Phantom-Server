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

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-secret-key-change-in-production-minimum-32-characters";
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
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// REMOVER esta línea que causa el warning:
// app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowAll");

// Ban Middleware
app.UseMiddleware<BanMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseStaticFiles();

app.Run("http://26.96.149.7:5000");
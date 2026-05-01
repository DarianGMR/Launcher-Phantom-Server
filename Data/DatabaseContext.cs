using Microsoft.EntityFrameworkCore;
using LauncherPhantomServer.Models;

namespace LauncherPhantomServer.Data
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Ban> Bans { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Configuración optimizada de User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
                
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);
                
                entity.Property(e => e.PasswordHash)
                    .IsRequired();
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                // ✅ Índices para búsquedas frecuentes
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.IsActive); // Para filtros de usuarios activos
                entity.HasIndex(e => e.CreatedAt); // Para ordenamiento por fecha

                // Relación con bans
                entity.HasMany(e => e.Bans)
                    .WithOne(b => b.User)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ✅ Configuración optimizada de Ban
            modelBuilder.Entity<Ban>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.IpAddress)
                    .IsRequired()
                    .HasMaxLength(45) // IPv4 + IPv6
                    .IsUnicode(false);
                
                entity.Property(e => e.Reason)
                    .IsRequired()
                    .HasMaxLength(500);
                
                entity.Property(e => e.BannedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.Property(e => e.IsPermanent)
                    .HasDefaultValue(false);

                // ✅ Índices críticos para bans
                entity.HasIndex(b => b.IpAddress);
                entity.HasIndex(b => b.UserId);
                entity.HasIndex(b => new { b.IsPermanent, b.ExpiresAt }); // Índice compuesto para búsquedas de bans activos
                entity.HasIndex(b => b.ExpiresAt); // Para cleanup automático
            });
        }
    }
}
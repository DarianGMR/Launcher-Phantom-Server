using LauncherPhantomServer.Data;
using LauncherPhantomServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LauncherPhantomServer.Services
{
    public class BanService
    {
        private readonly DatabaseContext _context;

        public BanService(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<bool> BanUserAsync(int userId, string ipAddress, string reason, int durationHours, int adminId)
        {
            try
            {
                var ban = new Ban
                {
                    UserId = userId,
                    IpAddress = ipAddress,
                    Reason = reason,
                    BannedAt = DateTime.UtcNow,
                    ExpiresAt = durationHours == 0 ? DateTime.MaxValue : DateTime.UtcNow.AddHours(durationHours),
                    IsPermanent = durationHours == 0,
                    BannedByAdminId = adminId
                };

                _context.Bans.Add(ban);
                
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsActive = false;
                    _context.Users.Update(user);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UnbanUserAsync(int banId)
        {
            try
            {
                var ban = await _context.Bans.FindAsync(banId);
                if (ban == null) return false;

                _context.Bans.Remove(ban);
                
                var user = await _context.Users.FindAsync(ban.UserId);
                if (user != null && !_context.Bans.Any(b => b.UserId == user.Id))
                {
                    user.IsActive = true;
                    _context.Users.Update(user);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Ban>> GetActiveBansAsync()
        {
            return await _context.Bans
                .Where(b => b.IsPermanent || b.ExpiresAt > DateTime.UtcNow)
                .Include(b => b.User)
                .ToListAsync();
        }

        public async Task<List<Ban>> GetUserBansAsync(int userId)
        {
            return await _context.Bans
                .Where(b => b.UserId == userId)
                .ToListAsync();
        }

        public async Task<Ban?> GetBanByIpAsync(string ipAddress)
        {
            return await _context.Bans
                .Where(b => b.IpAddress == ipAddress && (b.IsPermanent || b.ExpiresAt > DateTime.UtcNow))
                .FirstOrDefaultAsync();
        }
    }
}
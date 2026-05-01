namespace LauncherPhantomServer.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
        public string? LastIp { get; set; }

        public ICollection<Ban> Bans { get; set; } = new List<Ban>();
    }
}
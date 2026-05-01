namespace LauncherPhantomServer.Models
{
    public class Ban
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime BannedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsPermanent { get; set; } = false;
        public int BannedByAdminId { get; set; }

        public User User { get; set; } = null!;
    }
}
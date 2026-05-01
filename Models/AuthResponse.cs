namespace LauncherPhantomServer.Models
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
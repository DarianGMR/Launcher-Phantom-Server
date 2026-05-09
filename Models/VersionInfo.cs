namespace LauncherPhantomServer.Models
{
    public class VersionInfo
    {
        public string DownloadUrl { get; set; } = "http://0.0.0.0:5000/update/LauncherPhantom.exe";
        public string[] Changes { get; set; } = Array.Empty<string>();
        public bool Required { get; set; } = false;
    }
}
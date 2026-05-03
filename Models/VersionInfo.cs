namespace LauncherPhantomServer.Models
{
    public class VersionInfo
    {
        public string Version { get; set; } = "0.1.0";
        public string DownloadUrl { get; set; } = "http://0.0.0.0:5000/update/LauncherPhantom.exe";
        public string Changes { get; set; } = "- Version inicial";
        public bool Required { get; set; } = false;
    }
}
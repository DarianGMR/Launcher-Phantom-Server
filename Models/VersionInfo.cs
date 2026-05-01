namespace LauncherPhantomServer.Models
{
    public class VersionInfo
    {
        public string Version { get; set; } = "0.1.0";
        public string DownloadUrl { get; set; } = "http://26.96.149.7:5000/downloads/launcher-update.exe";
        public string Changes { get; set; } = "- Version inicial";
        public bool Required { get; set; } = false;
    }
}
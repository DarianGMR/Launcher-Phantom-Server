using System.Text.Json;
using LauncherPhantomServer.Models;

namespace LauncherPhantomServer.Services
{
    public class UpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private const string UPDATE_FOLDER = "Update";
        private const string UPDATE_FILE = "update.json";

        public UpdateService(ILogger<UpdateService> logger)
        {
            _logger = logger;
        }

        public void InitializeUpdateFolder()
        {
            try
            {
                if (!Directory.Exists(UPDATE_FOLDER))
                {
                    Directory.CreateDirectory(UPDATE_FOLDER);
                    _logger.LogInformation("[UpdateService] Carpeta Update creada");
                }

                var updateJsonPath = Path.Combine(UPDATE_FOLDER, UPDATE_FILE);
                
                if (!File.Exists(updateJsonPath))
                {
                    var defaultUpdate = new UpdateInfo
                    {
                        Version = "0.1.0",
                        DownloadUrl = "http://localhost:5000/update/LauncherPhantom.exe",
                        Changes = new[] { "Example 1",
                        "Example 2",
                        "Example 3" },
                        Required = false
                    };

                    var json = JsonSerializer.Serialize(defaultUpdate, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(updateJsonPath, json);
                    _logger.LogInformation("[UpdateService] archivo update.json creado");
                }
                else
                {
                    _logger.LogInformation("[UpdateService] archivo update.json ya existe");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateService] Error inicializando carpeta Update");
                throw;
            }
        }

        public async Task<UpdateInfo?> GetUpdateInfoAsync()
        {
            try
            {
                var updateJsonPath = Path.Combine(UPDATE_FOLDER, UPDATE_FILE);
                
                if (!File.Exists(updateJsonPath))
                {
                    _logger.LogWarning("[UpdateService] Archivo update.json no encontrado");
                    return null;
                }

                var json = await File.ReadAllTextAsync(updateJsonPath);
                var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json);
                
                _logger.LogInformation($"[UpdateService] Update info obtenido - Version: {updateInfo?.Version}");
                return updateInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateService] Error leyendo update.json");
                return null;
            }
        }

        public async Task<bool> UpdateVersionAsync(string version, string[] changes, bool required = false)
        {
            try
            {
                var updateInfo = new UpdateInfo
                {
                    Version = version,
                    DownloadUrl = $"http://localhost:5000/update/LauncherPhantom.exe",
                    Changes = changes,
                    Required = required
                };

                var updateJsonPath = Path.Combine(UPDATE_FOLDER, UPDATE_FILE);
                var json = JsonSerializer.Serialize(updateInfo, new JsonSerializerOptions { WriteIndented = true });
                
                await File.WriteAllTextAsync(updateJsonPath, json);
                
                _logger.LogInformation($"[UpdateService] Versión actualizada a {version}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateService] Error actualizando versión");
                return false;
            }
        }
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = "0.1.0";
        public string DownloadUrl { get; set; } = "";
        public string[] Changes { get; set; } = Array.Empty<string>();
        public bool Required { get; set; } = false;
    }
}
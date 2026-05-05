using System.Text.Json;
using LauncherPhantomServer.Models;

namespace LauncherPhantomServer.Services
{
    public class UpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly string UPDATE_FOLDER;
        private const string UPDATE_FILE = "update.json";

        public UpdateService(ILogger<UpdateService> logger)
        {
            _logger = logger;
            // Carpeta Update en la raíz del proyecto, no en wwwroot
            UPDATE_FOLDER = Path.Combine(Directory.GetCurrentDirectory(), "Update");
        }

        public void InitializeUpdateFolder()
        {
            try
            {
                if (!Directory.Exists(UPDATE_FOLDER))
                {
                    Directory.CreateDirectory(UPDATE_FOLDER);
                    _logger.LogInformation($"[UpdateService] Carpeta Update creada en: {UPDATE_FOLDER}");
                }

                var updateJsonPath = Path.Combine(UPDATE_FOLDER, UPDATE_FILE);
                
                if (!File.Exists(updateJsonPath))
                {
                    var defaultUpdate = new UpdateInfo
                    {
                        Version = "0.1.0",
                        DownloadUrl = "http://localhost:5000/update/LauncherPhantom.exe",
                        Changes = new[] { "EXAMPLE 1",
                        "EXAMPLE 2",
                        "EXAMPLE 3",
                        "EXAMPLE 4",
                        "EXAMPLE 5", },
                        Required = false
                    };

                    var json = JsonSerializer.Serialize(defaultUpdate, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(updateJsonPath, json);
                    _logger.LogInformation($"[UpdateService] Archivo update.json creado en: {updateJsonPath}");
                }
                else
                {
                    _logger.LogInformation($"[UpdateService] Archivo update.json ya existe en: {updateJsonPath}");
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
                    _logger.LogWarning($"[UpdateService] Archivo update.json no encontrado en: {updateJsonPath}");
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
                
                _logger.LogInformation($"[UpdateService] Versión actualizada a {version} en: {updateJsonPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateService] Error actualizando versión");
                return false;
            }
        }

        public string GetUpdateFolderPath()
        {
            return UPDATE_FOLDER;
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
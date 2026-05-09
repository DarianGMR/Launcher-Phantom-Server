using System.Text.Json;
using LauncherPhantomServer.Models;

namespace LauncherPhantomServer.Services
{
    public class UpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly string UPDATE_FOLDER;
        private const string UPDATE_FILE = "update.json";
        private readonly SemaphoreSlim _fileLock = new(1, 1);

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
                        Changes = new[] { 
                            "EXAMPLE 1",
                            "EXAMPLE 2",
                            "EXAMPLE 3",
                            "EXAMPLE 4",
                            "EXAMPLE 5"
                        },
                        Required = false
                    };

                    var json = JsonSerializer.Serialize(defaultUpdate, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(updateJsonPath, json);
                    _logger.LogInformation($"[UpdateService] Archivo update.json creado en: {updateJsonPath}");
                }
                else
                {
                    _logger.LogDebug($"[UpdateService] Archivo update.json ya existe");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateService] ERROR inicializando carpeta Update");
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
                    _logger.LogWarning($"[UpdateService] Archivo update.json no encontrado");
                    return null;
                }

                // Lock para evitar lectura concurrente mientras se escribe
                await _fileLock.WaitAsync();
                try
                {
                    var json = await File.ReadAllTextAsync(updateJsonPath);
                    var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json);
                    
                    if (updateInfo != null)
                    {
                        _logger.LogDebug($"[UpdateService] Versión actual: {updateInfo.Version}");
                    }
                    
                    return updateInfo;
                }
                finally
                {
                    _fileLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateService] ERROR leyendo update.json");
                return null;
            }
        }

        public async Task<bool> UpdateVersionAsync(string version, string[] changes, bool required = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(version))
                {
                    _logger.LogWarning("[UpdateService] Versión vacía");
                    return false;
                }

                var updateInfo = new UpdateInfo
                {
                    Version = version,
                    DownloadUrl = $"http://localhost:5000/update/LauncherPhantom.exe",
                    Changes = changes ?? Array.Empty<string>(),
                    Required = required
                };

                var updateJsonPath = Path.Combine(UPDATE_FOLDER, UPDATE_FILE);
                var json = JsonSerializer.Serialize(updateInfo, new JsonSerializerOptions { WriteIndented = true });
                
                // Lock para evitar escritura concurrente
                await _fileLock.WaitAsync();
                try
                {
                    await File.WriteAllTextAsync(updateJsonPath, json);
                    _logger.LogInformation($"[UpdateService] Versión actualizada a {version}");
                    return true;
                }
                finally
                {
                    _fileLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateService] ERROR actualizando versión");
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
        public string DownloadUrl { get; set; } = string.Empty;
        public string[] Changes { get; set; } = Array.Empty<string>();
        public bool Required { get; set; } = false;
    }
}
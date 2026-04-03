using System.Text.Json;

namespace Sts.Api.Services;

/// <summary>
/// Service singleton qui lit le data.json au démarrage et le sert en mémoire.
/// Le fichier est rechargé si sa date de modification change (sans redémarrage).
/// </summary>
public class DataService
{
    private readonly string _filePath;
    private readonly ILogger<DataService> _logger;

    private string _cachedJson = "{}";
    private DateTime _lastModified = DateTime.MinValue;
    private readonly object _lock = new();

    public DataService(IConfiguration configuration, ILogger<DataService> logger)
    {
        _logger = logger;

        var configured = configuration["Sts:DataFilePath"] ?? "data.json";

        // Chemin absolu ou relatif au répertoire de l'exe
        _filePath = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);

        _logger.LogInformation("DataService initialisé. Fichier : {Path}", _filePath);

        Reload();
    }

    /// <summary>
    /// Retourne le contenu JSON brut du data.json.
    /// Recharge automatiquement si le fichier a été modifié.
    /// </summary>
    public string GetRawJson()
    {
        TryReloadIfModified();
        return _cachedJson;
    }

    // --- privé ---

    private void TryReloadIfModified()
    {
        try
        {
            var lastWrite = File.GetLastWriteTimeUtc(_filePath);
            if (lastWrite <= _lastModified) return;

            lock (_lock)
            {
                // Double-check après acquisition du verrou
                lastWrite = File.GetLastWriteTimeUtc(_filePath);
                if (lastWrite <= _lastModified) return;

                Reload();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossible de vérifier la modification du fichier data.json.");
        }
    }

    private void Reload()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Fichier data.json introuvable : {Path}", _filePath);
                _cachedJson = "{}";
                return;
            }

            var raw = File.ReadAllText(_filePath);

            // Validation minimale : s'assure que c'est du JSON valide
            JsonDocument.Parse(raw);

            _cachedJson = raw;
            _lastModified = File.GetLastWriteTimeUtc(_filePath);

            _logger.LogInformation("data.json chargé ({Bytes} octets).", raw.Length);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "data.json invalide — contenu précédent conservé.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement de data.json.");
        }
    }
}

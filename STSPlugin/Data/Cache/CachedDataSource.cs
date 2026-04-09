using System;
using System.IO;
using System.Text.Json;
using Dalamud.Plugin.Services;

namespace STSPlugin.DataSource;

/// <summary>
/// Decorator de source de données avec stratégie de cache sur disque.
///
/// Stratégie au chargement :
///   1. Tente de récupérer le JSON depuis le back (RemoteJsonDataSource).
///      → Si succès : sauvegarde le JSON dans le fichier cache et retourne le modèle.
///   2. Si le back est inaccessible : lit le fichier cache s'il existe.
///   3. Si pas de cache : fallback sur la source locale (data.json bundlé avec le plugin).
/// </summary>
public class CachedDataSource : IDataSource
{
    private readonly RemoteJsonDataSource _remote;
    private readonly LocalJsonDataSource _fallback;
    private readonly string _cacheFilePath;
    private readonly IPluginLog _log;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <param name="remote">Source distante à interroger en priorité.</param>
    /// <param name="fallback">Source locale utilisée si le remote et le cache sont indisponibles.</param>
    /// <param name="cacheFilePath">Chemin du fichier cache écrit après chaque récupération distante réussie.</param>
    /// <param name="log">Logger Dalamud.</param>
    public CachedDataSource(
        RemoteJsonDataSource remote,
        LocalJsonDataSource fallback,
        string cacheFilePath,
        IPluginLog log)
    {
        _remote = remote;
        _fallback = fallback;
        _cacheFilePath = cacheFilePath;
        _log = log;
    }

    /// <inheritdoc/>
    public DataModel Load()
    {
        // 1. Tentative remote
        try
        {
            var json = _remote.FetchRawJson();
            SaveCache(json);
            var model = JsonSerializer.Deserialize<DataModel>(json, JsonOptions) ?? new DataModel();
            _log.Information("[STS] Données chargées depuis le back.");
            return model;
        }
        catch (Exception ex)
        {
            _log.Warning("[STS] Back inaccessible ({Message}). Tentative sur le cache.", ex.Message);
        }

        // 2. Tentative cache disque
        if (File.Exists(_cacheFilePath))
        {
            try
            {
                var json = File.ReadAllText(_cacheFilePath);
                var model = JsonSerializer.Deserialize<DataModel>(json, JsonOptions) ?? new DataModel();
                _log.Information("[STS] Données chargées depuis le cache.");
                return model;
            }
            catch (Exception ex)
            {
                _log.Warning("[STS] Cache illisible ({Message}). Fallback sur le fichier local.", ex.Message);
            }
        }

        // 3. Fallback local (data.json bundlé)
        _log.Warning("[STS] Fallback sur le data.json local.");
        return _fallback.Load();
    }

    // --- privé ---

    private void SaveCache(string json)
    {
        try
        {
            var dir = Path.GetDirectoryName(_cacheFilePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            _log.Warning("[STS] Impossible d'écrire le cache ({Message}).", ex.Message);
        }
    }
}

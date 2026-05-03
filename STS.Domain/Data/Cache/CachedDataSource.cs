using STS.Domain;
using System;
using System.IO;
using System.Text.Json;

namespace Sts.Domain.DataSource;

/// <summary>
/// Decorator de source de données avec stratégie de cache mémoire + disque.
///
/// Stratégie au chargement :
///   0. Si le cache mémoire est déjà rempli, le retourne directement (appels successifs idempotents).
///   1. Tente de récupérer le JSON depuis le back (RemoteJsonDataSource).
///      → Si succès : sauvegarde le JSON dans le fichier cache disque et retourne le modèle.
///   2. Si le back est inaccessible : lit le fichier cache disque s'il existe.
///   3. Si pas de cache disque : fallback sur la source locale (data.json bundlé avec le plugin).
///
/// Le cache mémoire est invalidé par <see cref="Invalidate"/> — appelé par <c>MainDiContainer.ReloadDataSources()</c>
/// avant de reconstruire les repositories.
/// </summary>
public class CachedDataSource : IDataSource
{
    private readonly RemoteJsonDataSource _remote;
    private readonly LocalJsonDataSource _fallback;
    private readonly string _cacheFilePath;
    private readonly IStsLogger _log;

    /// <summary>Cache mémoire — null tant que <see cref="Load"/> n'a pas été appelé (ou après <see cref="Invalidate"/>).</summary>
    private DataModel? _memoryCache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <param name="remote">Source distante à interroger en priorité.</param>
    /// <param name="fallback">Source locale utilisée si le remote et le cache disque sont indisponibles.</param>
    /// <param name="cacheFilePath">Chemin du fichier cache écrit après chaque récupération distante réussie.</param>
    /// <param name="log">Logger Dalamud.</param>
    public CachedDataSource(
        RemoteJsonDataSource remote,
        LocalJsonDataSource fallback,
        string cacheFilePath,
        IStsLogger log)
    {
        _remote = remote;
        _fallback = fallback;
        _cacheFilePath = cacheFilePath;
        _log = log;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Idempotent : les appels successifs retournent le cache mémoire sans I/O ni réseau.
    /// Appelez <see cref="Invalidate"/> avant de recharger si vous souhaitez forcer un nouveau chargement.
    /// </remarks>
    public DataModel Load()
    {
        // 0. Cache mémoire — court-circuit si déjà chargé
        if (_memoryCache != null)
            return _memoryCache;

        // 1. Tentative remote
        try
        {
            var json = _remote.FetchRawJson();
            SaveCache(json);
            _memoryCache = JsonSerializer.Deserialize<DataModel>(json, JsonOptions) ?? new DataModel();
            _log.Information("[STS] Données chargées depuis le back.");
            return _memoryCache;
        }
        catch (Exception ex)
        {
            _log.Warning("[STS] Back inaccessible ({Message}). Tentative sur le cache disque.", ex.Message);
        }

        // 2. Tentative cache disque
        if (File.Exists(_cacheFilePath))
        {
            try
            {
                var json = File.ReadAllText(_cacheFilePath);
                _memoryCache = JsonSerializer.Deserialize<DataModel>(json, JsonOptions) ?? new DataModel();
                _log.Information("[STS] Données chargées depuis le cache disque.");
                return _memoryCache;
            }
            catch (Exception ex)
            {
                _log.Warning("[STS] Cache disque illisible ({Message}). Fallback sur le fichier local.", ex.Message);
            }
        }

        // 3. Fallback local (data.json bundlé)
        _log.Warning("[STS] Fallback sur le data.json local.");
        _memoryCache = _fallback.Load();
        return _memoryCache;
    }

    /// <summary>
    /// Invalide le cache mémoire. Le prochain appel à <see cref="Load"/> relancera la séquence complète
    /// (remote → cache disque → local).
    /// </summary>
    public void Invalidate() => _memoryCache = null;

    // ── Privé ────────────────────────────────────────────────────────────────

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
            _log.Warning("[STS] Impossible d'écrire le cache disque ({Message}).", ex.Message);
        }
    }
}

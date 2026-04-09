using System.Net.Http.Json;
using System.Text.Json;
using STS.Web.Models;

namespace STS.Web.Services;

/// <summary>
/// Service client pour l'API STS — récupère les données de référence
/// depuis GET /api/data et les met en cache pour la durée de la session.
/// </summary>
public class StsDataService
{
    private readonly HttpClient _http;
    private DataModel? _cache;

    public StsDataService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Retourne les données de référence complètes.
    /// Le résultat est mis en cache après le premier appel.
    /// </summary>
    public async Task<DataModel> GetDataAsync()
    {
        if (_cache is not null)
            return _cache;

        var json = await _http.GetStringAsync("/api/data");

        _cache = JsonSerializer.Deserialize<DataModel>(json)
            ?? new DataModel();

        return _cache;
    }

    /// <summary>Invalide le cache — force un rechargement au prochain appel.</summary>
    public void InvalidateCache() => _cache = null;
}

using System.Net.Http.Json;
using System.Text.Json;
using Sts.Domain.DataSource;

namespace STS.Web.DataSource;

/// <summary>
/// Implémentation de <see cref="IDataSource"/> pour STS.Web.
/// Charge les données de référence depuis <c>GET /api/data</c> une fois au démarrage,
/// puis sert le cache mémoire de façon synchrone.
///
/// Usage dans Program.cs :
/// <code>
/// var host = builder.Build();
/// await host.Services.GetRequiredService&lt;HttpDataSource&gt;().LoadAsync();
/// await host.RunAsync();
/// </code>
/// </summary>
public class HttpDataSource : IDataSource
{
    private readonly HttpClient _http;
    private DataModel? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public HttpDataSource(HttpClient http) => _http = http;

    /// <summary>
    /// Retourne le modèle depuis le cache mémoire.
    /// Retourne un modèle vide si <see cref="LoadAsync"/> n'a pas encore été appelé.
    /// </summary>
    public DataModel Load() => _cache ?? new DataModel();

    /// <summary>
    /// Charge les données depuis <c>/api/data</c> et remplit le cache.
    /// Idempotent — les appels successifs retournent le cache sans I/O réseau.
    /// </summary>
    public async Task<DataModel> LoadAsync()
    {
        if (_cache is not null) return _cache;

        try
        {
            _cache = await _http.GetFromJsonAsync<DataModel>("/api/data", JsonOptions)
                     ?? new DataModel();
        }
        catch
        {
            // En cas d'échec (offline, etc.) on retourne un modèle vide
            // pour ne pas bloquer le démarrage de l'app
            _cache = new DataModel();
        }

        return _cache;
    }

    /// <summary>Invalide le cache — le prochain <see cref="LoadAsync"/> relancera la requête.</summary>
    public void Invalidate() => _cache = null;
}

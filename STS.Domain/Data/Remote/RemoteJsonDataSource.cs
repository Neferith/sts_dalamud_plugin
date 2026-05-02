using System;
using System.Net.Http;
using System.Text.Json;

namespace Sts.Domain.DataSource;

/// <summary>
/// Source de données distante : récupère le data.json depuis le back STS via HTTP.
/// Utilise un timeout court pour ne pas bloquer le démarrage du plugin si le back est indisponible.
/// </summary>
public class RemoteJsonDataSource : IDataSource
{
    private readonly string _url;
    private readonly TimeSpan _timeout;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <param name="url">URL complète de l'endpoint data (ex : http://localhost:50310/api/data).</param>
    /// <param name="timeoutSeconds">Timeout en secondes. Par défaut 5s.</param>
    public RemoteJsonDataSource(string url, int timeoutSeconds = 5)
    {
        _url = url;
        _timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    /// <summary>
    /// Tente de charger les données depuis le back.
    /// Lève une exception si le back est inaccessible ou retourne une réponse invalide.
    /// </summary>
    /// <exception cref="HttpRequestException">Back inaccessible ou réponse HTTP non-succès.</exception>
    /// <exception cref="JsonException">Réponse reçue mais JSON invalide.</exception>
    /// <exception cref="TimeoutException">Le back n'a pas répondu dans le délai imparti.</exception>
    public DataModel Load()
    {
        using var client = new HttpClient { Timeout = _timeout };

        // Appel synchrone — appelé une seule fois au démarrage du plugin
        var response = client.GetAsync(_url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return JsonSerializer.Deserialize<DataModel>(json, JsonOptions) ?? new DataModel();
    }

    /// <summary>
    /// Retourne le JSON brut depuis le back, sans désérialisation.
    /// Utilisé par le CachedDataSource pour écrire le cache sur disque.
    /// </summary>
    /// <exception cref="HttpRequestException">Back inaccessible ou réponse HTTP non-succès.</exception>
    /// <exception cref="TimeoutException">Le back n'a pas répondu dans le délai imparti.</exception>
    public string FetchRawJson()
    {
        using var client = new HttpClient { Timeout = _timeout };
        var response = client.GetAsync(_url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
}

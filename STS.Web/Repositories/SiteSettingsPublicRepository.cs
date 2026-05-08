using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;
using System.Net.Http.Json;

namespace STS.Web.Repositories;

/// <summary>Accès en lecture aux <see cref="SiteSettings"/> via l'API publique.</summary>
public sealed class SiteSettingsPublicRepository(HttpClient http) : ISiteSettingsReadRepository
{
    /// <inheritdoc/>
    public async Task<SiteSettings> GetAsync()
        => await http.GetFromJsonAsync<SiteSettings>("api/site-settings")
           ?? new SiteSettings();
}

using Sts.Domain.Content;
using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;
using Sts.Domain.Content.UseCases;
using System.Net;
using System.Net.Http.Json;

namespace STS.Admin.Repositories;
/// <inheritdoc cref="ISiteSettingsRepository"/>
public sealed class SiteSettingsRemoteRepository(HttpClient http) : ISiteSettingsRepository
{
    /// <inheritdoc/>
    public async Task<SiteSettings> GetAsync()
        => await http.GetFromJsonAsync<SiteSettings>("api/site-settings")
           ?? new SiteSettings();

    /// <inheritdoc/>
    public async Task<SiteSettings> SaveAsync(SiteSettings settings)
    {
        var response = await http.PutAsJsonAsync("api/site-settings", settings);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SiteSettings>())!;
    }
}

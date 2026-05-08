using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;
using Sts.Domain.Content.UseCases;
using System.Net;
using System.Net.Http.Json;

namespace STS.Admin.Repositories;

/// <inheritdoc cref="IQuickLinksRepository"/>
public sealed class QuickLinksRemoteRepository(HttpClient http) : IQuickLinksRepository
{
    /// <inheritdoc/>
    public async Task<IEnumerable<QuickLink>> GetAllAsync()
        => await http.GetFromJsonAsync<IEnumerable<QuickLink>>("api/quick-links/all")
           ?? [];

    /// <inheritdoc/>
    public async Task<QuickLink> AddAsync(CreateQuickLinkParameters parameters)
    {
        var response = await http.PostAsJsonAsync("api/quick-links", parameters);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<QuickLink>())!;
    }

    /// <inheritdoc/>
    public async Task<QuickLink?> UpdateAsync(Guid id, UpdateQuickLinkParameters parameters)
    {
        var response = await http.PutAsJsonAsync($"api/quick-links/{id}", parameters);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<QuickLink>();
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await http.DeleteAsync($"api/quick-links/{id}");
        return response.IsSuccessStatusCode;
    }
}

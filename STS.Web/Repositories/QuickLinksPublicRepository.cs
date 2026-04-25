using Sts.Domain.Content;
using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;
using System.Net.Http.Json;

namespace STS.Web.Repositories;

/// <summary>Accès en lecture aux <see cref="QuickLink"/> via l'API publique.</summary>
public sealed class QuickLinksPublicRepository(HttpClient http) : IQuickLinksReadRepository
{
    /// <inheritdoc/>
    public async Task<IEnumerable<QuickLink>> GetAllAsync()
        => await http.GetFromJsonAsync<IEnumerable<QuickLink>>("api/quick-links")
           ?? [];
}

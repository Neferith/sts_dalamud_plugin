using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;
using System.Net.Http.Json;

namespace STS.Web.Repositories;

public sealed class HomeCardPublicRepository(HttpClient http) : IHomeCardReadRepository
{
    public async Task<IReadOnlyList<HomeCard>> GetAllAsync() =>
        await http.GetFromJsonAsync<List<HomeCard>>("api/home-cards") ?? [];
}

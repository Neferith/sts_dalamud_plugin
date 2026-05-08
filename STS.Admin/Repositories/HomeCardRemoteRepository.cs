using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;
using System.Net.Http.Json;

namespace STS.Admin.Repositories;

/// <inheritdoc cref="IHomeCardRepository"/>
public sealed class HomeCardRemoteRepository(HttpClient http) : IHomeCardRepository
{
    public async Task<IReadOnlyList<HomeCard>> GetAllAsync()
        => await http.GetFromJsonAsync<List<HomeCard>>("api/home-cards/all") ?? [];

    public async Task<HomeCard?> GetByIdAsync(Guid id)
        => await http.GetFromJsonAsync<HomeCard>($"api/home-cards/{id}");

    public async Task<HomeCard> CreateAsync(HomeCard card)
    {
        var response = await http.PostAsJsonAsync("api/home-cards", card);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HomeCard>())!;
    }

    public async Task<HomeCard?> UpdateAsync(HomeCard card)
    {
        var response = await http.PutAsJsonAsync($"api/home-cards/{card.Id}", card);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HomeCard>();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await http.DeleteAsync($"api/home-cards/{id}");
        return response.IsSuccessStatusCode;
    }
}

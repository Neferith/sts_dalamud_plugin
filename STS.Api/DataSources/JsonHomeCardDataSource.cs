using System.Text.Json;
using Sts.Domain.Content.DataSources;
using Sts.Domain.Content.Models;

namespace Sts.Api.DataSources;

public sealed class JsonHomeCardDataSource : IHomeCardDataSource
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public JsonHomeCardDataSource(string filePath) => _filePath = filePath;

    public async Task<IReadOnlyList<HomeCard>> LoadAsync()
    {
        if (!File.Exists(_filePath)) return [];
        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<HomeCard>>(json, _opts) ?? [];
    }

    public async Task SaveAsync(IReadOnlyList<HomeCard> cards)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(cards, _opts));
    }
}

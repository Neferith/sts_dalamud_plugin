using System.Text.Json;
using Sts.Domain.Content;

namespace Sts.Api.Services;

/// <summary>
/// Charge et expose les sections de règles depuis <c>rules.json</c>
/// placé à la racine du projet API.
/// </summary>
public sealed class RulesService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IReadOnlyList<RulesSection> _sections;

    public RulesService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "rules.json");

        if (!File.Exists(path))
        {
            _sections = [];
            return;
        }

        var json = File.ReadAllText(path);
        _sections = JsonSerializer.Deserialize<List<RulesSection>>(json, _jsonOptions)
                        ?.OrderBy(s => s.Order).ToList()
                    ?? [];
    }

    /// <summary>Retourne toutes les sections triées par <see cref="RulesSection.Order"/>.</summary>
    public IReadOnlyList<RulesSection> GetAll() => _sections;
}

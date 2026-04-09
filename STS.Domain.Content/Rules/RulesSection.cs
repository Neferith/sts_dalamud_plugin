namespace Sts.Domain.Content;

/// <summary>Section thématique regroupant plusieurs articles de règles.</summary>
public sealed class RulesSection
{
    /// <summary>Identifiant unique (slug, ex : "guide-systeme").</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Titre affiché de la section.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Ordre d'affichage (croissant).</summary>
    public int Order { get; init; }

    /// <summary>Articles de la section, dans l'ordre de lecture.</summary>
    public List<RulesPost> Posts { get; init; } = [];
}

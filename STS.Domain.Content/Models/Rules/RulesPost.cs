namespace Sts.Domain.Content;

/// <summary>Article de règles appartenant à une section.</summary>
public sealed class RulesPost
{
    /// <summary>Identifiant unique (slug, ex : "systeme-tres-simple").</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Titre affiché dans la navigation et en en-tête.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Corps de l'article en Markdown.</summary>
    public string Content { get; init; } = string.Empty;
}

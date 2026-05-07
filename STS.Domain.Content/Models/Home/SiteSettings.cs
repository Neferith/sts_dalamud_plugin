namespace Sts.Domain.Content.Models;

/// <summary>Paramètres éditoriaux du site, configurables depuis l'admin.</summary>
public record SiteSettings
{
    /// <summary>Titre principal affiché dans le hero.</summary>
    public string HeroTitle { get; init; } = "Nouvelle Lune";

    /// <summary>Accroche courte affichée sous le titre hero.</summary>
    public string HeroTagline { get; init; } = "Compagnie Libre · Final Fantasy XIV";

    /// <summary>Texte de présentation de la guilde.</summary>
    public string HeroText { get; init; } = string.Empty;

    /// <summary>Monde FFXIV affiché dans le hero.</summary>
    public string World { get; init; } = string.Empty;

    /// <summary>Data center FFXIV affiché dans le hero.</summary>
    public string DataCenter { get; init; } = string.Empty;


}

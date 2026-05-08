using Sts.Domain.Content.Models;

namespace Sts.Domain.Content.DataSources;

/// <summary>Contrat d'accès brut aux données de la home (fichier, BDD, etc.).</summary>
public interface ISiteSettingsDataSource
{
    /// <summary>Charge et retourne les paramètres du site.</summary>
    Task<SiteSettings> LoadAsync();

    /// <summary>Persiste les paramètres du site.</summary>
    Task SaveAsync(SiteSettings siteSettings);
}


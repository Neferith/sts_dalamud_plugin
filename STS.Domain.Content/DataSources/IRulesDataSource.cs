namespace Sts.Domain.Content.DataSources;

/// <summary>Contrat d'accès brut aux données de règles (fichier, BDD, etc.).</summary>
public interface IRulesDataSource
{
    /// <summary>Charge et retourne la liste brute des sections.</summary>
    Task<List<RulesSection>> LoadAsync();

    /// <summary>Persiste la liste complète des sections.</summary>
    Task SaveAsync(List<RulesSection> sections);
}

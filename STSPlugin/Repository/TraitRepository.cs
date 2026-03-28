using System.Collections.Generic;
using System.Linq;
using STSPlugin.DataSource;
using STSPlugin.Domain;

namespace STSPlugin.Repository;

/// <summary>
/// Contrat d'accès aux traits du système.
/// </summary>
public interface TraitRepository
{
    /// <summary>Retourne tous les traits disponibles.</summary>
    IReadOnlyList<Trait> GetAll();

    /// <summary>Retourne un trait par son identifiant, ou null s'il n'existe pas.</summary>
    Trait? GetById(string id);

    /// <summary>Retourne tous les traits d'une catégorie donnée.</summary>
    IReadOnlyList<Trait> GetByCategory(TraitCategory category);

    /// <summary>Retourne tous les traits accessibles pour un job donné (null = traits sans job requis).</summary>
    IReadOnlyList<Trait> GetByJobId(string? jobId);
}

/// <summary>
/// Implémentation par défaut de <see cref="TraitRepository"/>.
/// Charge les données depuis la source et les conserve en cache mémoire.
/// </summary>
public class DefaultTraitRepository : TraitRepository
{
    private readonly IReadOnlyDictionary<string, Trait> _cache;

    public DefaultTraitRepository(IDataSource dataSource)
    {
        var data = dataSource.Load();
        _cache = data.Traits
            .Select(MapTrait)
            .ToDictionary(t => t.Id);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Trait> GetAll()
        => [.. _cache.Values];

    /// <inheritdoc/>
    public Trait? GetById(string id)
        => _cache.TryGetValue(id, out var trait) ? trait : null;

    /// <inheritdoc/>
    public IReadOnlyList<Trait> GetByCategory(TraitCategory category)
        => [.. _cache.Values.Where(t => t.Category == category)];

    /// <inheritdoc/>
    public IReadOnlyList<Trait> GetByJobId(string? jobId)
        => [.. _cache.Values.Where(t => t.RequiredJobId == null || t.RequiredJobId == jobId)];

    // --- privé ---

    private static Trait MapTrait(TraitData data) => new(
        Id: data.Id,
        Name: data.Name,
        Description: data.Description,
        Category: ParseCategory(data.Category),
        RequiredJobId: data.RequiredJobId,
        ExclusiveGroup: data.ExclusiveGroup
    );

    private static TraitCategory ParseCategory(string value) => value switch
    {
        "Origine" => TraitCategory.Origine,
        "Connaissance" => TraitCategory.Connaissance,
        "RoleDps" => TraitCategory.RoleDps,
        "RoleSoigneur" => TraitCategory.RoleSoigneur,
        "RoleTank" => TraitCategory.RoleTank,
        "Job" => TraitCategory.Job,
        _ => TraitCategory.Connaissance, // fallback safe
    };
}

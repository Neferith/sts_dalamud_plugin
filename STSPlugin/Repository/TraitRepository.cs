using System.Collections.Generic;
using System.Linq;
using STSPlugin.DataSource;
using Sts.Domain;

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
        => [.. _cache.Values.Where(t =>
            t.RequiredJobIds == null ||
            t.RequiredJobIds.Count == 0 ||
            (jobId != null && t.RequiredJobIds.Contains(jobId)))];

    // --- mapping ---

    private static Trait MapTrait(TraitData data)
    {
        // Support both legacy requiredJobId and new requiredJobIds
        List<string>? jobIds = null;
        if (data.RequiredJobIds != null && data.RequiredJobIds.Count > 0)
            jobIds = data.RequiredJobIds;
        else if (!string.IsNullOrEmpty(data.RequiredJobId))
            jobIds = [data.RequiredJobId];

        return new Trait(
            Id: data.Id,
            Name: data.Name,
            Description: data.Description,
            Category: ParseCategory(data.Category),
            RequiredJobIds: jobIds,
            ExclusiveGroup: data.ExclusiveGroup,
            Effects: data.Effects.Select(MapEffect).ToList()
        );
    }

    private static TraitEffect MapEffect(TraitEffectData data) => new(
        Type: ParseEffectType(data.Type),
        Value: data.Value,
        ForcedMode: data.ForcedMode != null ? ParseRollMode(data.ForcedMode) : null,
        Context: data.Context
    );

    private static TraitEffectType ParseEffectType(string value) => value switch
    {
        "BonusRerolls" => TraitEffectType.BonusRerolls,
        "BonusPalier" => TraitEffectType.BonusPalier,
        "ForceRollMode" => TraitEffectType.ForceRollMode,
        "BonusSuccessOnZero" => TraitEffectType.BonusSuccessOnZero,
        "BonusSuccessOnReroll" => TraitEffectType.BonusSuccessOnReroll,
        "BonusSuccess" => TraitEffectType.BonusSuccess,
        "MalusSuccess" => TraitEffectType.MalusSuccess,
        _ => TraitEffectType.Manual,
    };

    private static RollMode ParseRollMode(string value) => value switch
    {
        "Avantage" => RollMode.Avantage,
        "Desavantage" => RollMode.Desavantage,
        _ => RollMode.Normal,
    };

    private static TraitCategory ParseCategory(string value) => value switch
    {
        "Origine" => TraitCategory.Origine,
        "Connaissance" => TraitCategory.Connaissance,
        "RoleDps" => TraitCategory.RoleDps,
        "RoleSoigneur" => TraitCategory.RoleSoigneur,
        "RoleTank" => TraitCategory.RoleTank,
        "Job" => TraitCategory.Job,
        _ => TraitCategory.Connaissance,
    };
}

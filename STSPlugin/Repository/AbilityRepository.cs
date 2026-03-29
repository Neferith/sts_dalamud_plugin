using System.Collections.Generic;
using System.Linq;
using STSPlugin.DataSource;
using STSPlugin.Domain;

namespace STSPlugin.Repository;

/// <summary>
/// Contrat d'accès aux compétences du système.
/// </summary>
public interface AbilityRepository
{
    /// <summary>Retourne toutes les compétences disponibles.</summary>
    IReadOnlyList<Ability> GetAll();

    /// <summary>Retourne une compétence par son identifiant, ou null.</summary>
    Ability? GetById(string id);

    /// <summary>Retourne les compétences d'une catégorie donnée.</summary>
    IReadOnlyList<Ability> GetByCategory(AbilityCategory category);

    /// <summary>Retourne les compétences accessibles pour un job donné.</summary>
    IReadOnlyList<Ability> GetByJobId(string? jobId);

    /// <summary>Retourne les compétences d'arme (category = Weapon).</summary>
    IReadOnlyList<Ability> GetWeapons();
}

/// <summary>
/// Implémentation par défaut de <see cref="AbilityRepository"/>.
/// Cache mémoire chargé depuis la source de données.
/// </summary>
public class DefaultAbilityRepository : AbilityRepository
{
    private readonly IReadOnlyDictionary<string, Ability> _cache;

    public DefaultAbilityRepository(IDataSource dataSource)
    {
        var data = dataSource.Load();
        _cache = data.Abilities
            .Select(MapAbility)
            .ToDictionary(a => a.Id);
    }

    public IReadOnlyList<Ability> GetAll()
        => [.. _cache.Values];

    public Ability? GetById(string id)
        => _cache.TryGetValue(id, out var a) ? a : null;

    public IReadOnlyList<Ability> GetByCategory(AbilityCategory category)
        => [.. _cache.Values.Where(a => a.Category == category)];

    public IReadOnlyList<Ability> GetByJobId(string? jobId)
        => [.. _cache.Values.Where(a => a.RequiredJobId == null || a.RequiredJobId == jobId)];

    public IReadOnlyList<Ability> GetWeapons()
        => [.. _cache.Values.Where(a => a.Category == AbilityCategory.Weapon)];

    // --- mapping ---

    private static Ability MapAbility(AbilityData data) => new(
        Id: data.Id,
        Name: data.Name,
        Category: ParseCategory(data.Category),
        Levels: data.Levels.Select(l => new AbilityLevel(l.Level, l.Description)).ToList(),
        RequiredJobId: data.RequiredJobId,
        UsageLimit: ParseUsageLimit(data.UsageLimit),
        StartLevel: data.StartLevel
    );

    private static AbilityCategory ParseCategory(string value) => value switch
    {
        "Weapon" => AbilityCategory.Weapon,
        "RoleDps" => AbilityCategory.RoleDps,
        "RoleSoigneur" => AbilityCategory.RoleSoigneur,
        "RoleTank" => AbilityCategory.RoleTank,
        "Job" => AbilityCategory.Job,
        _ => AbilityCategory.RoleDps,
    };

    private static UsageLimit ParseUsageLimit(string? value) => value switch
    {
        "OncePerCombat" => UsageLimit.OncePerCombat,
        "TwicePerCombat" => UsageLimit.TwicePerCombat,
        "OncePerEvent" => UsageLimit.OncePerEvent,
        _ => UsageLimit.None,
    };
}

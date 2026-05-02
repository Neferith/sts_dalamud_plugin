namespace Sts.Domain;

/// <summary>Catégorie d'une compétence.</summary>
public enum AbilityCategory
{
    Weapon,
    RoleDps,
    RoleSoigneur,
    RoleTank,
    Job,
}

/// <summary>Limite d'utilisation d'une compétence.</summary>
public enum UsageLimit
{
    None,
    OncePerCombat,
    TwicePerCombat,
    OncePerEvent,
    TwicePerEvent,
    ThreeTimesPerEvent,
}

/// <summary>Description d'un niveau de compétence.</summary>
public record AbilityLevel(int Level, string Description);

/// <summary>
/// Représente une compétence du système STS.
/// Entité immuable chargée depuis la source de données.
/// </summary>
public record Ability(
    string Id,
    string Name,
    AbilityCategory Category,
    IReadOnlyList<AbilityLevel> Levels,

    /// <summary>
    /// Identifiants des jobs requis pour cette compétence.
    /// Null ou vide = accessible sans job.
    /// </summary>
    IReadOnlyList<string>? RequiredJobIds = null,

    UsageLimit UsageLimit = UsageLimit.None,
    int StartLevel = 1
)
{
    /// <summary>Niveau maximum de cette compétence.</summary>
    public int MaxLevel => Levels.Count > 0 ? Levels[^1].Level : 1;
}

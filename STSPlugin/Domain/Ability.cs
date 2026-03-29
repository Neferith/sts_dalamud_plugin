using System.Collections.Generic;

namespace STSPlugin.Domain;

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
    string? RequiredJobId = null,
    UsageLimit UsageLimit = UsageLimit.None,
    int StartLevel = 1
)
{
    public int MaxLevel => Levels.Count > 0 ? Levels[^1].Level : 1;
}

/// <summary>
/// Compétence équipée sur un personnage avec le niveau atteint.
/// Les points gratuits sont désormais portés par les Certifications.
/// </summary>
public class EquippedAbility
{
    public string AbilityId { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
}

using System.Collections.Generic;

namespace STSPlugin.Domain;

/// <summary>Type d'effet mécanique d'un trait sur les jets de dés.</summary>
public enum TraitEffectType
{
    /// <summary>Rerolls supplémentaires (permanent ou contextuel).</summary>
    BonusRerolls,
    /// <summary>Réduit le palier de réussite lors d'un reroll.</summary>
    BonusPalier,
    /// <summary>Impose un mode de jet (Avantage ou Désavantage).</summary>
    ForceRollMode,
    /// <summary>Ajoute des réussites si un 0 apparaît dans le jet.</summary>
    BonusSuccessOnZero,
    /// <summary>Ajoute des réussites sur un type de jet.</summary>
    BonusSuccess,
    /// <summary>Requiert des réussites supplémentaires sur un type de jet.</summary>
    MalusSuccess,
    /// <summary>
    /// Ajoute des réussites sur le résultat d'un reroll.
    /// S'applique après le lancer du reroll, pas avant.
    /// </summary>
    BonusSuccessOnReroll,
    /// <summary>Effet déclaratif — géré par le joueur ou le MJ.</summary>
    Manual,
}

/// <summary>Effet mécanique d'un trait.</summary>
public record TraitEffect(
    TraitEffectType Type,
    int Value = 0,
    RollMode? ForcedMode = null,
    string? Context = null
);

/// <summary>Catégorie d'un trait.</summary>
public enum TraitCategory
{
    Origine,
    Connaissance,
    RoleDps,
    RoleSoigneur,
    RoleTank,
    Job,
}

/// <summary>
/// Représente un trait du système STS.
/// Entité immuable chargée depuis la source de données.
/// </summary>
public record Trait(
    string Id,
    string Name,
    string Description,
    TraitCategory Category,

    /// <summary>
    /// Identifiants des jobs requis pour accéder à ce trait.
    /// Null ou liste vide = accessible sans job particulier.
    /// Plusieurs jobs = accessible à tous les jobs listés.
    /// </summary>
    IReadOnlyList<string>? RequiredJobIds = null,

    string? ExclusiveGroup = null,
    IReadOnlyList<TraitEffect>? Effects = null
);

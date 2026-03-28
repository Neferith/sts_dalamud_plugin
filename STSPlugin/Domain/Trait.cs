using System.Collections.Generic;

namespace STSPlugin.Domain;

/// <summary>
/// Type d'effet mécanique d'un trait sur les jets de dés.
/// </summary>
public enum TraitEffectType
{
    /// <summary>
    /// Rerolls supplémentaires.
    /// Peut être conditionnel si Context est renseigné (ex : "forestier", "intimidation").
    /// </summary>
    BonusRerolls,

    /// <summary>
    /// Réduit le palier de réussite de Value points lors d'un reroll.
    /// </summary>
    BonusPalier,

    /// <summary>
    /// Impose un mode de jet (Avantage ou Désavantage) sur un type de jet.
    /// Context précise le type (ex : "attaque", "defense", "discrétion").
    /// </summary>
    ForceRollMode,

    /// <summary>
    /// Ajoute Value réussites si un 0 (ou chiffre absent) apparaît dans le jet.
    /// Context précise le type de jet concerné (ex : "attaque", "defense").
    /// </summary>
    BonusSuccessOnZero,

    /// <summary>
    /// Ajoute Value réussites sur un type de jet.
    /// Context précise le type (ex : "attaque_magique", "intimidation").
    /// </summary>
    BonusSuccess,

    /// <summary>
    /// Requiert Value réussites supplémentaires sur un type de jet.
    /// Context précise le type (ex : "defense", "attaque_distance").
    /// </summary>
    MalusSuccess,

    /// <summary>
    /// Effet déclaratif géré par le joueur ou le MJ.
    /// Ne peut pas être appliqué automatiquement par l'engine.
    /// </summary>
    Manual,
}

/// <summary>
/// Effet mécanique d'un trait sur les jets de dés.
/// </summary>
/// <param name="Type">Type d'effet.</param>
/// <param name="Value">Valeur numérique de l'effet (bonus/malus). 0 par défaut.</param>
/// <param name="ForcedMode">Mode de jet imposé pour ForceRollMode. Null sinon.</param>
/// <param name="Context">
/// Contexte conditionnel de l'effet.
/// Null = effet permanent sans condition.
/// Ex : "forestier", "attaque_magique", "intimidation", "attaque", "defense".
/// </param>
public record TraitEffect(
    TraitEffectType Type,
    int Value = 0,
    RollMode? ForcedMode = null,
    string? Context = null
);

/// <summary>
/// Catégorie d'un trait, déterminant ses règles d'équipement.
/// </summary>
public enum TraitCategory
{
    /// <summary>Trait d'origine — gratuit, hors quota, un seul à la fois.</summary>
    Origine,
    /// <summary>Trait de connaissance — avantage sur des sujets précis.</summary>
    Connaissance,
    /// <summary>Trait de rôle DPS.</summary>
    RoleDps,
    /// <summary>Trait de rôle Soigneur.</summary>
    RoleSoigneur,
    /// <summary>Trait de rôle Tank.</summary>
    RoleTank,
    /// <summary>Trait de job — spécifique au job du personnage.</summary>
    Job,
}

/// <summary>
/// Représente un trait du système STS.
/// Entité immuable chargée depuis la source de données.
/// </summary>
public record Trait(
    /// <summary>Identifiant unique du trait (ex : "forestier").</summary>
    string Id,

    /// <summary>Nom affiché du trait.</summary>
    string Name,

    /// <summary>Description des effets du trait.</summary>
    string Description,

    /// <summary>Catégorie déterminant les règles d'équipement.</summary>
    TraitCategory Category,

    /// <summary>Identifiant du job requis. Null si aucun job requis.</summary>
    string? RequiredJobId = null,

    /// <summary>Groupe d'exclusivité. Null si aucune exclusivité.</summary>
    string? ExclusiveGroup = null,

    /// <summary>
    /// Effets mécaniques du trait applicables automatiquement par l'engine.
    /// Les effets de type Manual sont listés pour information mais ignorés par l'engine.
    /// </summary>
    IReadOnlyList<TraitEffect>? Effects = null
);

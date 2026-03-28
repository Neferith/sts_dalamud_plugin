namespace STSPlugin.Domain;

/// <summary>
/// Catégorie d'un trait, déterminant ses règles d'équipement.
/// Conservée en enum car ensemble fermé défini par les règles du système.
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

    /// <summary>
    /// Identifiant du job requis pour accéder à ce trait.
    /// Null si aucun job requis.
    /// </summary>
    string? RequiredJobId = null,

    /// <summary>
    /// Groupe d'exclusivité — un seul trait du même groupe peut être équipé.
    /// Null si aucune exclusivité.
    /// </summary>
    string? ExclusiveGroup = null
);

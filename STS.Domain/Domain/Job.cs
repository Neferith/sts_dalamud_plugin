namespace Sts.Domain;

/// <summary>
/// Représente un job jouable.
/// Entité immuable chargée depuis la source de données.
/// </summary>
public record Job(
    /// <summary>Identifiant unique du job (ex : "machiniste").</summary>
    string Id,

    /// <summary>Nom affiché du job.</summary>
    string Name,

    /// <summary>Description et spécificités du job.</summary>
    string? Description = null,

    /// <summary>
    /// Chemin relatif de l'icône du job (ex : "jobs/machiniste.png").
    /// Null si aucune icône n'a encore été uploadée.
    /// </summary>
    string? IconUrl = null
);

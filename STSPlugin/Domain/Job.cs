namespace STSPlugin.Domain;

/// <summary>
/// Représente un job jouable.
/// Entité immuable chargée depuis la source de données.
/// </summary>
public record Job(
    /// <summary>Identifiant unique du job (ex : "machiniste").</summary>
    string Id,

    /// <summary>Nom affiché du job.</summary>
    string Name
);

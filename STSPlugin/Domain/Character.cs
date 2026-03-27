using System;

namespace STSPlugin.Domain;

/// <summary>
/// Représente un personnage joueur avec son rang STS.
/// Entité identifiée par un GUID stable.
/// </summary>
public class Character
{
    /// <summary>Identifiant unique du personnage. Généré à la création, jamais modifié.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom du personnage tel qu'il apparaît dans le jeu.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Rang STS actuel du personnage.</summary>
    public RankKey Rank { get; set; } = RankKey.Novice;
}

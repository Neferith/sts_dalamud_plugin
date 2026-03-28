using System;
using System.Collections.Generic;

namespace STSPlugin.Domain;

/// <summary>
/// Représente un personnage joueur avec son rang, son job et ses traits.
/// Les traits et le job sont référencés par identifiant string,
/// résolus via les repositories au moment de l'affichage ou de la validation.
/// </summary>
public class Character
{
    /// <summary>Identifiant unique du personnage. Généré à la création, jamais modifié.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom du personnage.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Rang STS actuel du personnage.</summary>
    public RankKey RankKey { get; set; } = RankKey.Novice;

    /// <summary>
    /// Identifiant du job du personnage.
    /// Null si aucun job assigné.
    /// </summary>
    public string? JobId { get; set; } = null;

    /// <summary>
    /// Identifiant du trait d'origine équipé.
    /// Gratuit, hors quota, un seul à la fois.
    /// Null si aucun trait d'origine équipé.
    /// </summary>
    public string? OriginTraitId { get; set; } = null;

    /// <summary>
    /// Identifiants des traits équipés (hors trait d'origine).
    /// La taille maximale est définie par le rang.
    /// </summary>
    public List<string> EquippedTraitIds { get; set; } = [];

    // --- propriétés calculées ---

    /// <summary>Nombre de slots de traits disponibles selon le rang.</summary>
    public int TraitSlots => Rank.Get(RankKey).Traits;

    /// <summary>Nombre de slots de traits encore libres.</summary>
    public int FreeTraitSlots => TraitSlots - EquippedTraitIds.Count;

    /// <summary>Indique si un trait (par id) est actuellement équipé.</summary>
    public bool HasTrait(string traitId)
        => EquippedTraitIds.Contains(traitId) || OriginTraitId == traitId;
}

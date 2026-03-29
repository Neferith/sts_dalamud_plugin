using System;
using System.Collections.Generic;

namespace STSPlugin.Domain;

/// <summary>
/// Représente un personnage joueur avec son rang, son job, ses traits et ses actions.
/// </summary>
public class Character
{
    /// <summary>Identifiant unique du personnage. Généré à la création, jamais modifié.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom du personnage.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Rang STS actuel du personnage.</summary>
    public RankKey RankKey { get; set; } = RankKey.Novice;

    /// <summary>Identifiant du job du personnage. Null si aucun job assigné.</summary>
    public string? JobId { get; set; } = null;

    /// <summary>
    /// Identifiant du trait d'origine équipé.
    /// Gratuit, hors quota, un seul à la fois. Null si aucun.
    /// </summary>
    public string? OriginTraitId { get; set; } = null;

    /// <summary>
    /// Identifiants des traits équipés (hors trait d'origine).
    /// Taille maximale définie par le rang.
    /// </summary>
    public List<string> EquippedTraitIds { get; set; } = [];

    /// <summary>
    /// Actions de jet personnalisées créées par le joueur.
    /// S'ajoutent aux actions prédéfinies du data.json.
    /// </summary>
    public List<RollAction> CustomActions { get; set; } = [];

    /// <summary>
    /// Ids des actions affichées dans la quickbar.
    /// Si vide, toutes les actions disponibles sont affichées.
    /// </summary>
    public List<string> QuickbarActionIds { get; set; } = [];

    // --- propriétés calculées ---

    /// <summary>Nombre de slots de traits disponibles selon le rang.</summary>
    public int TraitSlots => Rank.Get(RankKey).Traits;

    /// <summary>Nombre de slots de traits encore libres.</summary>
    public int FreeTraitSlots => TraitSlots - EquippedTraitIds.Count;

    /// <summary>Indique si un trait (par id) est actuellement équipé.</summary>
    public bool HasTrait(string traitId)
        => EquippedTraitIds.Contains(traitId) || OriginTraitId == traitId;
}

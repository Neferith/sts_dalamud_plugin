using System;
using System.Collections.Generic;
using System.Linq;

namespace STSPlugin.Domain;

/// <summary>
/// Représente un personnage joueur avec son rang, son job et ses traits.
/// </summary>
public class Character
{
    /// <summary>Identifiant unique du personnage. Généré à la création, jamais modifié.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom du personnage tel qu'il apparaît dans le jeu.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Rang STS actuel du personnage.</summary>
    public RankKey RankKey { get; set; } = RankKey.Novice;

    /// <summary>
    /// Job du personnage. Détermine les traits de job accessibles.
    /// </summary>
    public Job Job { get; set; } = Job.Aucun;

    /// <summary>
    /// Trait d'origine équipé. Gratuit, hors quota, un seul à la fois.
    /// Nécessite la certification associée (vérification MJ, pas codée).
    /// Null si aucun trait d'origine équipé.
    /// </summary>
    public TraitId? OriginTrait { get; set; } = null;

    /// <summary>
    /// Traits équipés (hors trait d'origine).
    /// La taille maximale est définie par <see cref="Rank.Get(RankKey).Traits"/>.
    /// </summary>
    public List<TraitId> EquippedTraits { get; set; } = [];

    // --- propriétés calculées ---

    /// <summary>Nombre de slots de traits disponibles selon le rang.</summary>
    public int TraitSlots => Rank.Get(RankKey).Traits;

    /// <summary>Nombre de slots de traits encore libres.</summary>
    public int FreeTraitSlots => TraitSlots - EquippedTraits.Count;

    /// <summary>Indique si un trait est actuellement équipé.</summary>
    public bool HasTrait(TraitId id)
        => EquippedTraits.Contains(id) || OriginTrait == id;

    /// <summary>
    /// Indique si un trait peut être équipé selon les règles :
    /// - Slots disponibles
    /// - Pas déjà équipé
    /// - Groupe d'exclusivité respecté
    /// - Job requis respecté
    /// </summary>
    public bool CanEquip(TraitId id)
    {
        if (HasTrait(id)) return false;
        if (FreeTraitSlots <= 0) return false;

        var trait = Trait.Get(id);

        if (trait.RequiredJob != null && trait.RequiredJob != Job)
            return false;

        if (trait.ExclusiveGroup != null)
        {
            var conflict = EquippedTraits.Any(e =>
                Trait.Get(e).ExclusiveGroup == trait.ExclusiveGroup);
            if (conflict) return false;
        }

        return true;
    }
}

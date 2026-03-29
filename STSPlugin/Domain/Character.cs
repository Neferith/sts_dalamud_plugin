using System;
using System.Collections.Generic;
using System.Linq;

namespace STSPlugin.Domain;

/// <summary>
/// Représente un personnage joueur.
/// </summary>
public class Character
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public RankKey RankKey { get; set; } = RankKey.Novice;
    public string? JobId { get; set; } = null;

    /// <summary>Trait d'origine équipé. Gratuit si une certification le débloque.</summary>
    public string? OriginTraitId { get; set; } = null;

    /// <summary>Traits équipés (hors trait d'origine). Max = Rank.Traits.</summary>
    public List<string> EquippedTraitIds { get; set; } = [];

    /// <summary>Actions personnalisées du joueur.</summary>
    public List<RollAction> CustomActions { get; set; } = [];

    /// <summary>Ids des actions visibles dans la quickbar. Vide = toutes.</summary>
    public List<string> QuickbarActionIds { get; set; } = [];

    /// <summary>Points de compétence accordés par le MJ.</summary>
    public int SkillPoints { get; set; } = 0;

    /// <summary>Compétences apprises.</summary>
    public List<EquippedAbility> EquippedAbilities { get; set; } = [];

    /// <summary>Certifications accordées par un officier.</summary>
    public List<Certification> Certifications { get; set; } = [];

    /// <summary>Inventaire du personnage (armes et objets divers).</summary>
    public List<CharacterItem> Inventory { get; set; } = [];

    // --- helpers inventaire ---

    /// <summary>Retourne les armes équipées.</summary>
    public IEnumerable<CharacterItem> EquippedWeapons
        => Inventory.Where(i => i.Category == ItemCategory.Weapon && i.IsEquipped);

    /// <summary>
    /// Indique si une arme équipée n'a pas la compétence requise (palier → 8).
    /// </summary>
    public bool IsWeaponUnmastered(CharacterItem weapon)
        => weapon.Category == ItemCategory.Weapon
        && weapon.LinkedAbilityId != null
        && GetAbilityLevel(weapon.LinkedAbilityId) == 0;

    // --- helpers traits ---

    public int TraitSlots => Rank.Get(RankKey).Traits;
    public int FreeTraitSlots => TraitSlots - EquippedTraitIds.Count;

    public bool HasTrait(string traitId)
        => EquippedTraitIds.Contains(traitId) || OriginTraitId == traitId;

    /// <summary>
    /// Indique si le trait d'origine peut être équipé gratuitement
    /// grâce à une certification.
    /// </summary>
    public bool HasCertificationForOriginTrait(string traitId)
        => Certifications.Any(c => c.LinkedOriginTraitId == traitId);

    // --- helpers compétences ---

    /// <summary>Points gratuits accordés par les certifications pour une compétence.</summary>
    public int GetFreePointsForAbility(string abilityId)
        => Certifications
            .Where(c => c.LinkedAbilityId == abilityId)
            .Sum(c => c.FreePoints);

    /// <summary>Points dépensés (net des points gratuits de certifications).</summary>
    public int SpentSkillPoints
        => EquippedAbilities.Sum(a => Math.Max(0, a.Level - GetFreePointsForAbility(a.AbilityId)));

    /// <summary>Points restants disponibles.</summary>
    public int RemainingSkillPoints => Math.Max(0, SkillPoints - SpentSkillPoints);

    /// <summary>Niveau atteint pour une compétence, ou 0 si non apprise.</summary>
    public int GetAbilityLevel(string abilityId)
        => EquippedAbilities.FirstOrDefault(a => a.AbilityId == abilityId)?.Level ?? 0;

    /// <summary>
    /// Nombre de compétences atteignant ou dépassant le niveau donné,
    /// en ne comptant que les niveaux payés (hors points gratuits).
    /// </summary>
    public int CountAbilitiesAtLevel(int level)
        => EquippedAbilities.Count(a =>
        {
            var paid = a.Level - GetFreePointsForAbility(a.AbilityId);
            return paid >= level;
        });
}

using Sts.Domain;

namespace Sts.Domain.Character;

/// <summary>
/// Représente un personnage joueur.
/// </summary>
public class Character
{
    /// <summary>Identifiant unique du personnage.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom du personnage.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Rang STS actuel.</summary>
    public RankKey RankKey { get; set; } = RankKey.Novice;

    /// <summary>Identifiant du job actif. Null si aucun job sélectionné.</summary>
    public string? JobId { get; set; } = null;

    /// <summary>Race du personnage.</summary>
    public CharacterRace Race { get; set; } = CharacterRace.Hyur;

    /// <summary>Texte libre — résumé de l'histoire du personnage.</summary>
    public string Histoire { get; set; } = string.Empty;

    /// <summary>Niveau de réputation, borné entre -5 et 10.</summary>
    public int ReputationLevel { get; set; } = 0;

    /// <summary>
    /// Identifiant du joueur propriétaire de ce personnage.
    /// Null pour les personnages créés localement dans le plugin (rétrocompatibilité).
    /// </summary>
    public Guid? PlayerId { get; set; } = null;

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

    /// <summary>Id de l'arme équipée en main principale. Null si vide.</summary>
    public string? MainHandItemId { get; set; } = null;

    /// <summary>Id de l'arme équipée en main secondaire. Null si vide.</summary>
    public string? OffHandItemId { get; set; } = null;

    // ── Helpers inventaire ────────────────────────────────────────────────────

    /// <summary>Retourne les armes actuellement équipées.</summary>
    public IEnumerable<CharacterItem> EquippedWeapons
        => Inventory.Where(i => i.Category == ItemCategory.Weapon && i.IsEquipped);

    /// <summary>
    /// Indique si une arme équipée n'a pas la compétence requise (palier → 8).
    /// </summary>
    /// <param name="weapon">L'arme à vérifier.</param>
    public bool IsWeaponUnmastered(CharacterItem weapon)
        => weapon.Category == ItemCategory.Weapon
        && weapon.LinkedAbilityId != null
        && GetAbilityLevel(weapon.LinkedAbilityId) == 0;

    // ── Helpers traits ────────────────────────────────────────────────────────

    /// <summary>Nombre de slots de traits disponibles selon le rang.</summary>
    public int TraitSlots => Rank.Get(RankKey).Traits;

    /// <summary>Nombre de slots de traits libres.</summary>
    public int FreeTraitSlots => TraitSlots - EquippedTraitIds.Count;

    /// <summary>Indique si le personnage possède le trait donné (équipé ou origine).</summary>
    /// <param name="traitId">Identifiant du trait.</param>
    public bool HasTrait(string traitId)
        => EquippedTraitIds.Contains(traitId) || OriginTraitId == traitId;

    /// <summary>
    /// Indique si le trait d'origine peut être équipé gratuitement
    /// grâce à une certification.
    /// </summary>
    /// <param name="traitId">Identifiant du trait d'origine.</param>
    public bool HasCertificationForOriginTrait(string traitId)
        => Certifications.Any(c => c.LinkedOriginTraitId == traitId);

    // ── Helpers compétences ───────────────────────────────────────────────────

    /// <summary>Points gratuits accordés par les certifications pour une compétence.</summary>
    /// <param name="abilityId">Identifiant de la compétence.</param>
    public int GetFreePointsForAbility(string abilityId)
        => Certifications
            .Where(c => c.LinkedAbilityId == abilityId)
            .Sum(c => c.FreePoints);

    /// <summary>Points de compétence dépensés (net des points gratuits de certifications).</summary>
    public int SpentSkillPoints
        => EquippedAbilities.Sum(a => Math.Max(0, a.Level - GetFreePointsForAbility(a.AbilityId)));

    /// <summary>Points de compétence restants disponibles.</summary>
    public int RemainingSkillPoints => Math.Max(0, SkillPoints - SpentSkillPoints);

    /// <summary>Niveau atteint pour une compétence, ou 0 si non apprise.</summary>
    /// <param name="abilityId">Identifiant de la compétence.</param>
    public int GetAbilityLevel(string abilityId)
        => EquippedAbilities.FirstOrDefault(a => a.AbilityId == abilityId)?.Level ?? 0;

    /// <summary>
    /// Nombre de compétences atteignant ou dépassant le niveau donné,
    /// en ne comptant que les niveaux payés (hors points gratuits).
    /// </summary>
    /// <param name="level">Niveau minimum à compter.</param>
    public int CountAbilitiesAtLevel(int level)
        => EquippedAbilities.Count(a =>
        {
            var paid = a.Level - GetFreePointsForAbility(a.AbilityId);
            return paid >= level;
        });
}

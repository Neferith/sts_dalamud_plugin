using System.Collections.Generic;
using System.Linq;

namespace STSPlugin.OldDomain;

/// <summary>
/// Catégorie d'un trait, déterminant son origine et ses règles d'équipement.
/// </summary>
public enum TraitCategory
{
    /// <summary>Trait d'origine — gratuit, hors quota, un seul à la fois, nécessite la certification associée.</summary>
    Origine,

    /// <summary>Trait de connaissance — confère l'avantage sur des sujets précis.</summary>
    Connaissance,

    /// <summary>Trait de rôle DPS — modificateurs offensifs.</summary>
    RoleDps,

    /// <summary>Trait de rôle Soigneur — modificateurs de soin et soutien.</summary>
    RoleSoigneur,

    /// <summary>Trait de rôle Tank — modificateurs défensifs.</summary>
    RoleTank,

    /// <summary>Trait de job — spécifique au job du personnage.</summary>
    Job,
}

/// <summary>
/// Identifiant unique et typé de chaque trait du système.
/// Ensemble fermé — tout trait inconnu est rejeté à la compilation.
/// </summary>
public enum TraitId
{
    // --- Origine ---
    Forestier,
    Urbain,
    Maritime,
    Aride,
    Campagnard,
    Glacial,
    Montagnard,
    Souterrain,
    Noble,
    Pauvre,
    Parieur,
    Vagabond,
    Uldien,
    Sharlayanais,
    Thavnairois,
    Garlemaldais,
    Saltimbanque,
    XaelaDesSteppes,

    // --- Connaissance ---
    ConnaissanceDeLaVie,
    ConnaissanceDeLaTechnologie,
    ConnaissanceDuMonde,
    ConnaissanceDeLArcane,

    // --- Rôle DPS ---
    SpécialisteDeLaMagie,
    SpécialisteDuContact,
    SpécialisteADistance,
    Polyviolence,
    Polyvalence,
    AttaquesCoordonnées,
    CoupCritique,

    // --- Rôle Soigneur ---
    AuraApaisante,
    PrestanceDuPrêtre,
    DéfenseCoordonnée,
    AssistanceHéroïque,

    // --- Rôle Tank ---
    AuraDeTémérité,
    AuraDeProtecteur,
    DéfenseCritique,
    ContrôleDuCombat,

    // --- Job Machiniste ---
    Pistolero,
    BouclierMécanique,
    TirDeCouverture,

    // --- Job Sage ---
    Kardia,
    DiplômeSharlayannais,
    BileDeVipère,

    // --- Job Faucheur ---
    EntrainementDAssassin,
    PrésenceTerrifiante,
    EnduranceMentale,

    // --- Job Guerrier ---
    BainDeSang,
    FrissonDeLaBataille,
    InsensibleÀLaDouleur,
}

/// <summary>
/// Jobs disponibles. Un job débloque une liste de traits spécifiques.
/// </summary>
public enum Job
{
    Aucun,
    Machiniste,
    Sage,
    Faucheur,
    Guerrier,
}

/// <summary>
/// Définition immuable d'un trait.
/// </summary>
/// <param name="Id">Identifiant unique du trait.</param>
/// <param name="Name">Nom affiché.</param>
/// <param name="Description">Description des effets du trait.</param>
/// <param name="Category">Catégorie déterminant les règles d'équipement.</param>
/// <param name="RequiredJob">Job requis pour accéder à ce trait. Null si aucun job requis.</param>
/// <param name="ExclusiveGroup">
/// Groupe d'exclusivité — un seul trait du même groupe peut être équipé simultanément.
/// Null si aucune exclusivité.
/// </param>
public record Trait(
    TraitId Id,
    string Name,
    string Description,
    TraitCategory Category,
    Job? RequiredJob = null,
    string? ExclusiveGroup = null)
{
    /// <summary>
    /// Catalogue complet de tous les traits du système.
    /// Source de vérité unique.
    /// </summary>
    public static readonly IReadOnlyDictionary<TraitId, Trait> All = new Dictionary<TraitId, Trait>
    {
        // ------------------------------------------------------------------ Origine
        [TraitId.Forestier] = new(TraitId.Forestier, "Forestier", "Reroll supplémentaire en environnement forestier.", TraitCategory.Origine),
        [TraitId.Urbain] = new(TraitId.Urbain, "Urbain", "Reroll supplémentaire en environnement urbain.", TraitCategory.Origine),
        [TraitId.Maritime] = new(TraitId.Maritime, "Maritime", "Reroll supplémentaire en environnement maritime.", TraitCategory.Origine),
        [TraitId.Aride] = new(TraitId.Aride, "Aride", "Reroll supplémentaire en environnement aride.", TraitCategory.Origine),
        [TraitId.Campagnard] = new(TraitId.Campagnard, "Campagnard", "Reroll supplémentaire en environnement campagnard.", TraitCategory.Origine),
        [TraitId.Glacial] = new(TraitId.Glacial, "Glacial", "Reroll supplémentaire en environnement glacial.", TraitCategory.Origine),
        [TraitId.Montagnard] = new(TraitId.Montagnard, "Montagnard", "Reroll supplémentaire en environnement montagnard.", TraitCategory.Origine),
        [TraitId.Souterrain] = new(TraitId.Souterrain, "Souterrain", "Reroll supplémentaire en environnement souterrain.", TraitCategory.Origine),
        [TraitId.Noble] = new(TraitId.Noble, "Noble", "Reroll supplémentaire en environnement aisé.", TraitCategory.Origine),
        [TraitId.Pauvre] = new(TraitId.Pauvre, "Pauvre", "Reroll supplémentaire en environnement démuni (camps de réfugiés, bas-fonds...).", TraitCategory.Origine),
        [TraitId.Parieur] = new(TraitId.Parieur, "Parieur", "Une fois par event en cas d'échec, relancez un dé 3. Le résultat devient votre nombre de réussites.", TraitCategory.Origine),
        [TraitId.Vagabond] = new(TraitId.Vagabond, "Vagabond", "Quand vous utilisez un reroll, diminuez votre palier de réussite de 1 pour ce reroll. Non cumulable.", TraitCategory.Origine),
        [TraitId.Uldien] = new(TraitId.Uldien, "Ul'dien", "Reroll supplémentaire sur les jets liés au marchandage.", TraitCategory.Origine),
        [TraitId.Sharlayanais] = new(TraitId.Sharlayanais, "Sharlayanais", "Reroll supplémentaire sur les jets liés à l'éthérologie.", TraitCategory.Origine),
        [TraitId.Thavnairois] = new(TraitId.Thavnairois, "Thavnairois", "Reroll supplémentaire sur les jets liés à l'alchimie.", TraitCategory.Origine),
        [TraitId.Garlemaldais] = new(TraitId.Garlemaldais, "Garlemaldais", "Reroll supplémentaire sur les jets liés à l'ingénierie magitek.", TraitCategory.Origine),
        [TraitId.Saltimbanque] = new(TraitId.Saltimbanque, "Saltimbanque", "Reroll supplémentaire sur les jets liés au déguisement ou à la tromperie.", TraitCategory.Origine),
        [TraitId.XaelaDesSteppes] = new(TraitId.XaelaDesSteppes, "Xaela des Steppes", "Reroll supplémentaire sur les jets liés à l'intimidation.", TraitCategory.Origine),

        // ------------------------------------------------------------------ Connaissance
        [TraitId.ConnaissanceDeLaVie] = new(TraitId.ConnaissanceDeLaVie, "Connaissance de la Vie", "Avantage sur : Médecine, Biologie, Poisons, Faune, Flore.", TraitCategory.Connaissance),
        [TraitId.ConnaissanceDeLaTechnologie] = new(TraitId.ConnaissanceDeLaTechnologie, "Connaissance de la Technologie", "Avantage sur : Machines, Réparations, Modifications, Crochetage, Piratage.", TraitCategory.Connaissance),
        [TraitId.ConnaissanceDuMonde] = new(TraitId.ConnaissanceDuMonde, "Connaissance du Monde", "Avantage sur : Traditions, Administration, Géographie, Criminalité, Politique.", TraitCategory.Connaissance),
        [TraitId.ConnaissanceDeLArcane] = new(TraitId.ConnaissanceDeLArcane, "Connaissance de l'Arcane", "Avantage sur : Éthérologie, Histoire, Écoles de magie.", TraitCategory.Connaissance),

        // ------------------------------------------------------------------ Rôle DPS
        [TraitId.SpécialisteDeLaMagie] = new(TraitId.SpécialisteDeLaMagie, "Spécialiste de la magie", "+1 réussite attaques magiques. +1 réussite requise autres attaques et défenses non-magiques. Incompatible avec les autres spécialistes.", TraitCategory.RoleDps, ExclusiveGroup: "Spécialiste"),
        [TraitId.SpécialisteDuContact] = new(TraitId.SpécialisteDuContact, "Spécialiste du contact", "+1 réussite attaques mêlée. +1 réussite requise autres attaques et défenses hors mêlée. Incompatible avec les autres spécialistes.", TraitCategory.RoleDps, ExclusiveGroup: "Spécialiste"),
        [TraitId.SpécialisteADistance] = new(TraitId.SpécialisteADistance, "Spécialiste à distance", "+1 réussite attaques à distance. +1 réussite requise autres attaques et défenses hors distance. Incompatible avec les autres spécialistes.", TraitCategory.RoleDps, ExclusiveGroup: "Spécialiste"),
        [TraitId.Polyviolence] = new(TraitId.Polyviolence, "Polyviolence", "+1 réussite jets d'attaque. +1 réussite requise jets de défense.", TraitCategory.RoleDps),
        [TraitId.Polyvalence] = new(TraitId.Polyvalence, "Polyvalence", "Reroll supplémentaire.", TraitCategory.RoleDps),
        [TraitId.AttaquesCoordonnées] = new(TraitId.AttaquesCoordonnées, "Attaques coordonnées", "Vous pouvez offrir un de vos rerolls à un allié sur un jet d'attaque.", TraitCategory.RoleDps),
        [TraitId.CoupCritique] = new(TraitId.CoupCritique, "Coup critique", "+1 réussite si un 0 (ou chiffre absent) apparaît dans un jet d'attaque. Incompatible avec Défense Critique.", TraitCategory.RoleDps, ExclusiveGroup: "Critique"),

        // ------------------------------------------------------------------ Rôle Soigneur
        [TraitId.AuraApaisante] = new(TraitId.AuraApaisante, "Aura apaisante", "Une fois par event, annule l'impact de blessure d'un joueur proche.", TraitCategory.RoleSoigneur),
        [TraitId.PrestanceDuPrêtre] = new(TraitId.PrestanceDuPrêtre, "Prestance du Prêtre", "Avantage jets d'attaque, mais impossible d'utiliser sorts curatifs ou boucliers. Déclaré en début de combat.", TraitCategory.RoleSoigneur),
        [TraitId.DéfenseCoordonnée] = new(TraitId.DéfenseCoordonnée, "Défense Coordonnée", "Vous pouvez offrir un de vos rerolls à un allié sur un jet de défense.", TraitCategory.RoleSoigneur),
        [TraitId.AssistanceHéroïque] = new(TraitId.AssistanceHéroïque, "Assistance héroïque", "Une fois par combat, si un allié échoue un jet de défense, lancez un dé et ajoutez max 2 réussites à son résultat.", TraitCategory.RoleSoigneur),

        // ------------------------------------------------------------------ Rôle Tank
        [TraitId.AuraDeTémérité] = new(TraitId.AuraDeTémérité, "Aura de témérité", "Attaque avec avantage, défense avec désavantage. Déclaré en début de combat.", TraitCategory.RoleTank),
        [TraitId.AuraDeProtecteur] = new(TraitId.AuraDeProtecteur, "Aura de protecteur", "Une fois par event, encaissez une blessure à la place d'un allié à proximité.", TraitCategory.RoleTank),
        [TraitId.DéfenseCritique] = new(TraitId.DéfenseCritique, "Défense Critique", "Un 0 (ou chiffre absent) sur un jet de défense ajoute une réussite. Incompatible avec Coup Critique.", TraitCategory.RoleTank, ExclusiveGroup: "Critique"),
        [TraitId.ContrôleDuCombat] = new(TraitId.ContrôleDuCombat, "Contrôle du combat", "Une fois par event, appliquez vos bonus de défense à un jet d'attaque.", TraitCategory.RoleTank),

        // ------------------------------------------------------------------ Job Machiniste
        [TraitId.Pistolero] = new(TraitId.Pistolero, "Pistolero", "+1 réussite jets d'attaque contre des cibles à distance. Désavantage sur jets de défense.", TraitCategory.Job, RequiredJob: Job.Machiniste),
        [TraitId.BouclierMécanique] = new(TraitId.BouclierMécanique, "Bouclier mécanique", "+1 réussite défense (soi ou allié) par tourelle à proximité. Les tourelles sont détruites. Reine = +2.", TraitCategory.Job, RequiredJob: Job.Machiniste),
        [TraitId.TirDeCouverture] = new(TraitId.TirDeCouverture, "Tir de Couverture", "+1 réussite jet allié de votre choix tant que vous n'attaquez pas. Une fois par combat, cumulable sur un même allié.", TraitCategory.Job, RequiredJob: Job.Machiniste),

        // ------------------------------------------------------------------ Job Sage
        [TraitId.Kardia] = new(TraitId.Kardia, "Kardia", "Une fois par event, applique Kardia à un équipier. Réussir une attaque réduit sa blessure d'un niveau (hors létale).", TraitCategory.Job, RequiredJob: Job.Sage),
        [TraitId.DiplômeSharlayannais] = new(TraitId.DiplômeSharlayannais, "Diplôme Sharlayannais", "+1 réussite sur les jets de connaissance effectués avec avantage.", TraitCategory.Job, RequiredJob: Job.Sage),
        [TraitId.BileDeVipère] = new(TraitId.BileDeVipère, "Bile de Vipère", "Une fois par event, ignore les modificateurs négatifs sur vos jets de dés.", TraitCategory.Job, RequiredJob: Job.Sage),

        // ------------------------------------------------------------------ Job Faucheur
        [TraitId.EntrainementDAssassin] = new(TraitId.EntrainementDAssassin, "Entraînement d'assassin", "Avantage sur les jets de discrétion et attaques surprises. +1 réussite requise sur jets de défense.", TraitCategory.Job, RequiredJob: Job.Faucheur),
        [TraitId.PrésenceTerrifiante] = new(TraitId.PrésenceTerrifiante, "Présence terrifiante", "+1 réussite sur les jets d'intimidation.", TraitCategory.Job, RequiredJob: Job.Faucheur),
        [TraitId.EnduranceMentale] = new(TraitId.EnduranceMentale, "Endurance Mentale", "+1 réussite contre les attaques mentales.", TraitCategory.Job, RequiredJob: Job.Faucheur),

        // ------------------------------------------------------------------ Job Guerrier
        [TraitId.BainDeSang] = new(TraitId.BainDeSang, "Bain de Sang", "Une fois par event, en réussissant une attaque, diminue le niveau de votre blessure selon vos succès. Déclaré avant le lancer.", TraitCategory.Job, RequiredJob: Job.Guerrier),
        [TraitId.FrissonDeLaBataille] = new(TraitId.FrissonDeLaBataille, "Frisson de la Bataille", "Avantage en nombre inférieur, désavantage en nombre supérieur. Déclaré en début de combat.", TraitCategory.Job, RequiredJob: Job.Guerrier),
        [TraitId.InsensibleÀLaDouleur] = new(TraitId.InsensibleÀLaDouleur, "Insensible à la douleur", "Palier de défense réduit de moitié (arrondi au supérieur) contre les créatures de grande taille.", TraitCategory.Job, RequiredJob: Job.Guerrier),
    };

    /// <summary>Récupère un trait par son identifiant.</summary>
    public static Trait Get(TraitId id) => All[id];

    /// <summary>Retourne tous les traits disponibles pour un job donné (hors job = traits communs).</summary>
    public static IEnumerable<Trait> GetByJob(Job job)
        => All.Values.Where(t => t.RequiredJob == job || t.RequiredJob == null);

    /// <summary>Retourne tous les traits d'une catégorie.</summary>
    public static IEnumerable<Trait> GetByCategory(TraitCategory category)
        => All.Values.Where(t => t.Category == category);
}

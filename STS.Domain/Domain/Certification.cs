using System;

namespace Sts.Domain;

/// <summary>
/// Certification accordée par un officier à un personnage.
/// Peut débloquer un trait d'origine gratuitement et/ou accorder
/// des points gratuits sur une arme.
/// </summary>
public class Certification
{
    /// <summary>Identifiant unique de cette certification (guid).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Nom affiché (ex : "Machiniste — Arme à feu", "Enfant des bois").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Identifiant du trait d'origine débloqué par cette certification.
    /// Null si cette certification ne débloque pas de trait d'origine.
    /// </summary>
    public string? LinkedOriginTraitId { get; set; } = null;

    /// <summary>
    /// Identifiant de la compétence d'arme sur laquelle cette certification
    /// accorde des points gratuits.
    /// Null si cette certification ne concerne pas une arme.
    /// </summary>
    public string? LinkedAbilityId { get; set; } = null;

    /// <summary>
    /// Nombre de points gratuits accordés sur la compétence liée.
    /// 0 si cette certification ne concerne pas une arme.
    /// </summary>
    public int FreePoints { get; set; } = 0;
}

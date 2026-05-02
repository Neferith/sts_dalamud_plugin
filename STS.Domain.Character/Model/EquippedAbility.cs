namespace Sts.Domain.Character;

/// <summary>Compétence équipée sur un personnage, avec le niveau atteint.</summary>
public class EquippedAbility
{
    /// <summary>Identifiant de la compétence (ex : "arme_a_feu").</summary>
    public string AbilityId { get; set; } = string.Empty;

    /// <summary>Niveau atteint (1–3).</summary>
    public int Level { get; set; } = 1;
}

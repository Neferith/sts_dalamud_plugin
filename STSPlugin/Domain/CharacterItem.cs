namespace STSPlugin.Domain;

/// <summary>Catégorie d'un objet d'inventaire.</summary>
public enum ItemCategory
{
    /// <summary>Arme — peut être équipée, liée à une compétence d'arme.</summary>
    Weapon,
    /// <summary>Objet divers — purement descriptif.</summary>
    Item,
}

/// <summary>
/// Objet dans l'inventaire d'un personnage.
/// Créé librement par le joueur, stocké sur le personnage.
/// </summary>
public class CharacterItem
{
    /// <summary>Identifiant unique (guid).</summary>
    public string Id { get; set; } = System.Guid.NewGuid().ToString();

    /// <summary>Nom de l'objet.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description libre (RP, apparence, provenance...).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Catégorie de l'objet.</summary>
    public ItemCategory Category { get; set; } = ItemCategory.Item;

    /// <summary>
    /// Identifiant de la compétence d'arme liée.
    /// Null si l'objet n'est pas une arme ou si aucune compétence n'est associée.
    /// Si le personnage n'a pas cette compétence (niveau 0), le palier d'attaque passe à 8.
    /// </summary>
    public string? LinkedAbilityId { get; set; } = null;

    /// <summary>
    /// Indique si l'arme est actuellement équipée.
    /// Sans effet pour les objets de catégorie Item.
    /// </summary>
    public bool IsEquipped { get; set; } = false;
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Sts.Domain.Character;

namespace STSPlugin.CharacterUseCases;

/// <summary>Slot d'équipement.</summary>
public enum EquipSlot { MainHand, OffHand }

/// <summary>Cas d'usage : ajouter un objet à l'inventaire.</summary>
public interface AddInventoryItemUseCase
{
    Task<CharacterItem> ExecuteAsync(
        Character character,
        string name,
        string description,
        ItemCategory category,
        string? linkedAbilityId = null,
        uint iconId = 21001);
}

/// <summary>Implémentation par défaut de <see cref="AddInventoryItemUseCase"/>.</summary>
public class DefaultAddInventoryItemUseCase : AddInventoryItemUseCase
{
    private readonly ICharacterRepository _repo;

    public DefaultAddInventoryItemUseCase(ICharacterRepository repo) => _repo = repo;

    /// <inheritdoc/>
    public async Task<CharacterItem> ExecuteAsync(
        Character character,
        string name,
        string description,
        ItemCategory category,
        string? linkedAbilityId = null,
        uint iconId = 21001)
    {
        var item = new CharacterItem
        {
            Name            = name.Trim(),
            Description     = description.Trim(),
            Category        = category,
            LinkedAbilityId = linkedAbilityId,
            IsEquipped      = false,
            IconId          = iconId,
            SortIndex       = character.Inventory.Count,
        };

        character.Inventory.Add(item);
        await _repo.SaveAsync(character);
        return item;
    }
}

/// <summary>Cas d'usage : retirer un objet de l'inventaire.</summary>
public interface RemoveInventoryItemUseCase
{
    Task ExecuteAsync(Character character, string itemId);
}

/// <summary>Implémentation par défaut de <see cref="RemoveInventoryItemUseCase"/>.</summary>
public class DefaultRemoveInventoryItemUseCase : RemoveInventoryItemUseCase
{
    private readonly ICharacterRepository _repo;

    public DefaultRemoveInventoryItemUseCase(ICharacterRepository repo) => _repo = repo;

    /// <inheritdoc/>
    public async Task ExecuteAsync(Character character, string itemId)
    {
        if (character.MainHandItemId == itemId) character.MainHandItemId = null;
        if (character.OffHandItemId  == itemId) character.OffHandItemId  = null;

        var removed = character.Inventory.RemoveAll(i => i.Id == itemId) > 0;
        if (!removed) return;

        for (var i = 0; i < character.Inventory.Count; i++)
            character.Inventory[i].SortIndex = i;

        await _repo.SaveAsync(character);
    }
}

/// <summary>Cas d'usage : assigner ou vider un slot d'équipement (main / secondaire).</summary>
public interface SetItemSlotUseCase
{
    /// <summary>
    /// Place l'arme dans le slot. Passer null vide le slot.
    /// L'arme est automatiquement marquée IsEquipped si elle est dans un slot.
    /// </summary>
    Task ExecuteAsync(Character character, EquipSlot slot, string? itemId);
}

/// <summary>Implémentation par défaut de <see cref="SetItemSlotUseCase"/>.</summary>
public class DefaultSetItemSlotUseCase : SetItemSlotUseCase
{
    private readonly ICharacterRepository _repo;

    public DefaultSetItemSlotUseCase(ICharacterRepository repo) => _repo = repo;

    /// <inheritdoc/>
    public async Task ExecuteAsync(Character character, EquipSlot slot, string? itemId)
    {
        if (itemId != null)
        {
            var item = character.Inventory.FirstOrDefault(i => i.Id == itemId);
            if (item is null || item.Category != ItemCategory.Weapon) return;
        }

        var oldId = slot == EquipSlot.MainHand ? character.MainHandItemId : character.OffHandItemId;
        if (oldId != null)
        {
            var old = character.Inventory.FirstOrDefault(i => i.Id == oldId);
            if (old != null)
                old.IsEquipped = character.MainHandItemId == oldId || character.OffHandItemId == oldId
                    ? false
                    : old.IsEquipped;
        }

        if (slot == EquipSlot.MainHand) character.MainHandItemId = itemId;
        else                            character.OffHandItemId  = itemId;

        foreach (var i in character.Inventory)
            i.IsEquipped = i.Id == character.MainHandItemId || i.Id == character.OffHandItemId;

        await _repo.SaveAsync(character);
    }
}

/// <summary>Cas d'usage : réordonner l'inventaire (drag &amp; drop).</summary>
public interface ReorderInventoryUseCase
{
    /// <summary>Déplace l'objet de fromIndex vers toIndex.</summary>
    Task ExecuteAsync(Character character, int fromIndex, int toIndex);
}

/// <summary>Implémentation par défaut de <see cref="ReorderInventoryUseCase"/>.</summary>
public class DefaultReorderInventoryUseCase : ReorderInventoryUseCase
{
    private readonly ICharacterRepository _repo;

    public DefaultReorderInventoryUseCase(ICharacterRepository repo) => _repo = repo;

    /// <inheritdoc/>
    public async Task ExecuteAsync(Character character, int fromIndex, int toIndex)
    {
        var inv = character.Inventory;
        if (fromIndex < 0 || fromIndex >= inv.Count) return;
        if (toIndex   < 0 || toIndex   >= inv.Count) return;
        if (fromIndex == toIndex) return;

        var item = inv[fromIndex];
        inv.RemoveAt(fromIndex);
        inv.Insert(toIndex, item);

        for (var i = 0; i < inv.Count; i++)
            inv[i].SortIndex = i;

        await _repo.SaveAsync(character);
    }
}

/// <summary>Cas d'usage : mettre à jour l'icône d'un objet.</summary>
public interface SetItemIconUseCase
{
    Task ExecuteAsync(Character character, string itemId, uint iconId);
}

/// <summary>Implémentation par défaut de <see cref="SetItemIconUseCase"/>.</summary>
public class DefaultSetItemIconUseCase : SetItemIconUseCase
{
    private readonly ICharacterRepository _repo;

    public DefaultSetItemIconUseCase(ICharacterRepository repo) => _repo = repo;

    /// <inheritdoc/>
    public async Task ExecuteAsync(Character character, string itemId, uint iconId)
    {
        var item = character.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;

        item.IconId = iconId;
        await _repo.SaveAsync(character);
    }
}

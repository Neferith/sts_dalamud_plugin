using System;
using System.Linq;
using STSPlugin.Domain;
using STSPlugin.Repository;

namespace STSPlugin.UseCases;

/// <summary>Slot d'équipement.</summary>
public enum EquipSlot { MainHand, OffHand }

/// <summary>Cas d'usage : ajouter un objet à l'inventaire.</summary>
public interface AddInventoryItemUseCase
{
    CharacterItem Execute(
        Character character,
        string name,
        string description,
        ItemCategory category,
        string? linkedAbilityId = null,
        uint iconId = 21001);
}

public class DefaultAddInventoryItemUseCase : AddInventoryItemUseCase
{
    private readonly CharacterRepository _repo;
    public DefaultAddInventoryItemUseCase(CharacterRepository repo) => _repo = repo;

    public CharacterItem Execute(Character character, string name, string description,
        ItemCategory category, string? linkedAbilityId = null, uint iconId = 21001)
    {
        var item = new CharacterItem
        {
            Name = name.Trim(),
            Description = description.Trim(),
            Category = category,
            LinkedAbilityId = linkedAbilityId,
            IsEquipped = false,
            IconId = iconId,
            SortIndex = character.Inventory.Count,
        };
        character.Inventory.Add(item);
        _repo.Save(character);
        return item;
    }
}

/// <summary>Cas d'usage : retirer un objet de l'inventaire.</summary>
public interface RemoveInventoryItemUseCase
{
    void Execute(Character character, string itemId);
}

public class DefaultRemoveInventoryItemUseCase : RemoveInventoryItemUseCase
{
    private readonly CharacterRepository _repo;
    public DefaultRemoveInventoryItemUseCase(CharacterRepository repo) => _repo = repo;

    public void Execute(Character character, string itemId)
    {
        // Vider le slot si l'arme y était équipée
        if (character.MainHandItemId == itemId) character.MainHandItemId = null;
        if (character.OffHandItemId == itemId) character.OffHandItemId = null;

        var removed = character.Inventory.RemoveAll(i => i.Id == itemId) > 0;
        if (!removed) return;

        // Réindexer
        for (var i = 0; i < character.Inventory.Count; i++)
            character.Inventory[i].SortIndex = i;

        _repo.Save(character);
    }
}

/// <summary>Cas d'usage : assigner ou vider un slot d'équipement (main / secondaire).</summary>
public interface SetItemSlotUseCase
{
    /// <summary>
    /// Place l'arme dans le slot. Passer null vide le slot.
    /// L'arme est automatiquement marquée IsEquipped si elle est dans un slot.
    /// </summary>
    void Execute(Character character, EquipSlot slot, string? itemId);
}

public class DefaultSetItemSlotUseCase : SetItemSlotUseCase
{
    private readonly CharacterRepository _repo;
    public DefaultSetItemSlotUseCase(CharacterRepository repo) => _repo = repo;

    public void Execute(Character character, EquipSlot slot, string? itemId)
    {
        // Valider que l'item est bien une arme
        if (itemId != null)
        {
            var item = character.Inventory.FirstOrDefault(i => i.Id == itemId);
            if (item is null || item.Category != ItemCategory.Weapon) return;
        }

        // Vider l'ancien slot
        var oldId = slot == EquipSlot.MainHand ? character.MainHandItemId : character.OffHandItemId;
        if (oldId != null)
        {
            var old = character.Inventory.FirstOrDefault(i => i.Id == oldId);
            if (old != null)
                old.IsEquipped = character.MainHandItemId == oldId || character.OffHandItemId == oldId
                    ? false
                    : old.IsEquipped;
        }

        // Assigner le nouveau
        if (slot == EquipSlot.MainHand) character.MainHandItemId = itemId;
        else character.OffHandItemId = itemId;

        // Mettre à jour IsEquipped
        foreach (var i in character.Inventory)
            i.IsEquipped = i.Id == character.MainHandItemId || i.Id == character.OffHandItemId;

        _repo.Save(character);
    }
}

/// <summary>Cas d'usage : réordonner l'inventaire (drag & drop).</summary>
public interface ReorderInventoryUseCase
{
    /// <summary>Déplace l'objet de fromIndex vers toIndex.</summary>
    void Execute(Character character, int fromIndex, int toIndex);
}

public class DefaultReorderInventoryUseCase : ReorderInventoryUseCase
{
    private readonly CharacterRepository _repo;
    public DefaultReorderInventoryUseCase(CharacterRepository repo) => _repo = repo;

    public void Execute(Character character, int fromIndex, int toIndex)
    {
        var inv = character.Inventory;
        if (fromIndex < 0 || fromIndex >= inv.Count) return;
        if (toIndex < 0 || toIndex >= inv.Count) return;
        if (fromIndex == toIndex) return;

        var item = inv[fromIndex];
        inv.RemoveAt(fromIndex);
        inv.Insert(toIndex, item);

        for (var i = 0; i < inv.Count; i++)
            inv[i].SortIndex = i;

        _repo.Save(character);
    }
}

/// <summary>Cas d'usage : mettre à jour l'icône d'un objet.</summary>
public interface SetItemIconUseCase
{
    void Execute(Character character, string itemId, uint iconId);
}

public class DefaultSetItemIconUseCase : SetItemIconUseCase
{
    private readonly CharacterRepository _repo;
    public DefaultSetItemIconUseCase(CharacterRepository repo) => _repo = repo;

    public void Execute(Character character, string itemId, uint iconId)
    {
        var item = character.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;
        item.IconId = iconId;
        _repo.Save(character);
    }
}

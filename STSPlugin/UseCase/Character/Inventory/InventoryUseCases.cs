using System;
using System.Linq;
using STSPlugin.Domain;
using STSPlugin.Repository;

namespace STSPlugin.UseCases;

/// <summary>Cas d'usage : ajouter un objet à l'inventaire.</summary>
public interface AddCharacterItemUseCase
{
    CharacterItem Execute(
        Character character,
        string name,
        string description,
        ItemCategory category,
        string? linkedAbilityId = null);
}

public class DefaultAddCharacterItemUseCase : AddCharacterItemUseCase
{
    private readonly CharacterRepository _repo;
    public DefaultAddCharacterItemUseCase(CharacterRepository repo) => _repo = repo;

    public CharacterItem Execute(Character character, string name, string description, ItemCategory category, string? linkedAbilityId = null)
    {
        var item = new CharacterItem
        {
            Name = name.Trim(),
            Description = description.Trim(),
            Category = category,
            LinkedAbilityId = linkedAbilityId,
            IsEquipped = false,
        };
        character.Inventory.Add(item);
        _repo.Save(character);
        return item;
    }
}

/// <summary>Cas d'usage : retirer un objet de l'inventaire.</summary>
public interface RemoveCharacterItemUseCase
{
    void Execute(Character character, string itemId);
}

public class DefaultRemoveCharacterItemUseCase : RemoveCharacterItemUseCase
{
    private readonly CharacterRepository _repo;
    public DefaultRemoveCharacterItemUseCase(CharacterRepository repo) => _repo = repo;

    public void Execute(Character character, string itemId)
    {
        var removed = character.Inventory.RemoveAll(i => i.Id == itemId) > 0;
        if (removed) _repo.Save(character);
    }
}

/// <summary>Cas d'usage : équiper ou déséquiper une arme.</summary>
public interface ToggleEquipItemUseCase
{
    void Execute(Character character, string itemId);
}

public class DefaultToggleEquipItemUseCase : ToggleEquipItemUseCase
{
    private readonly CharacterRepository _repo;
    public DefaultToggleEquipItemUseCase(CharacterRepository repo) => _repo = repo;

    public void Execute(Character character, string itemId)
    {
        var item = character.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (item is null || item.Category != ItemCategory.Weapon) return;

        item.IsEquipped = !item.IsEquipped;
        _repo.Save(character);
    }
}

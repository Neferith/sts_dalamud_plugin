using System.Linq;
using STSPlugin.Domain;
using STSPlugin.Repository;

namespace STSPlugin.UseCases;

/// <summary>Résultat de la tentative d'équipement d'une compétence.</summary>
public enum EquipAbilityResult
{
    Success,
    AbilityNotFound,
    NotEnoughPoints,
    RankTooLow,
    AlreadyMaxLevel,
    PreviousLevelRequired,
    LevelCapReached,
}

/// <summary>
/// Cas d'usage : apprendre ou monter de niveau une compétence.
/// Les points gratuits sont lus depuis les certifications du personnage.
/// </summary>
public interface EquipAbilityUseCase
{
    EquipAbilityResult Execute(Character character, string abilityId, int targetLevel);
}

public class DefaultEquipAbilityUseCase : EquipAbilityUseCase
{
    private readonly CharacterRepository _characterRepository;
    private readonly AbilityRepository _abilityRepository;

    public DefaultEquipAbilityUseCase(CharacterRepository characterRepository, AbilityRepository abilityRepository)
    {
        _characterRepository = characterRepository;
        _abilityRepository = abilityRepository;
    }

    public EquipAbilityResult Execute(Character character, string abilityId, int targetLevel)
    {
        var ability = _abilityRepository.GetById(abilityId);
        if (ability is null) return EquipAbilityResult.AbilityNotFound;

        var rank = Rank.Get(character.RankKey);
        var currentLevel = character.GetAbilityLevel(abilityId);
        var freePoints = character.GetFreePointsForAbility(abilityId);

        if (currentLevel >= ability.MaxLevel)
            return EquipAbilityResult.AlreadyMaxLevel;

        if (targetLevel > ability.StartLevel && currentLevel < targetLevel - 1)
            return EquipAbilityResult.PreviousLevelRequired;

        // Les points gratuits de certification ignorent les caps de rang
        var paidLevel = System.Math.Max(0, targetLevel - freePoints);
        if (paidLevel > 0 && !rank.AllowsAbilityLevel(targetLevel))
            return EquipAbilityResult.RankTooLow;

        // Cap niveau 2 — ne compte que les niveaux payants
        if (targetLevel == 2 && rank.MaxAbilityLv2 != -1)
        {
            if (character.CountAbilitiesAtLevel(2) >= rank.MaxAbilityLv2)
                return EquipAbilityResult.LevelCapReached;
        }
        // Cap niveau 3
        if (targetLevel == 3 && rank.MaxAbilityLv3 != -1)
        {
            if (character.CountAbilitiesAtLevel(3) >= rank.MaxAbilityLv3)
                return EquipAbilityResult.LevelCapReached;
        }

        // Points disponibles — on recalcule après changement de niveau
        var currentPaid = System.Math.Max(0, currentLevel - freePoints);
        var targetPaid = System.Math.Max(0, targetLevel - freePoints);
        var additionalCost = targetPaid - currentPaid;

        if (additionalCost > character.RemainingSkillPoints)
            return EquipAbilityResult.NotEnoughPoints;

        // Appliquer
        var equipped = character.EquippedAbilities.FirstOrDefault(a => a.AbilityId == abilityId);
        if (equipped is null)
            character.EquippedAbilities.Add(new EquippedAbility { AbilityId = abilityId, Level = targetLevel });
        else
            equipped.Level = targetLevel;

        _characterRepository.Save(character);
        return EquipAbilityResult.Success;
    }
}

/// <summary>Cas d'usage : retirer une compétence apprise.</summary>
public interface UnequipAbilityUseCase
{
    void Execute(Character character, string abilityId);
}

public class DefaultUnequipAbilityUseCase : UnequipAbilityUseCase
{
    private readonly CharacterRepository _characterRepository;

    public DefaultUnequipAbilityUseCase(CharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    public void Execute(Character character, string abilityId)
    {
        var removed = character.EquippedAbilities.RemoveAll(a => a.AbilityId == abilityId) > 0;
        if (removed) _characterRepository.Save(character);
    }
}

/// <summary>Cas d'usage : définir les points de compétence accordés par le MJ.</summary>
public interface SetSkillPointsUseCase
{
    void Execute(Character character, int points);
}

public class DefaultSetSkillPointsUseCase : SetSkillPointsUseCase
{
    private readonly CharacterRepository _characterRepository;

    public DefaultSetSkillPointsUseCase(CharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    public void Execute(Character character, int points)
    {
        character.SkillPoints = System.Math.Max(0, points);
        _characterRepository.Save(character);
    }
}

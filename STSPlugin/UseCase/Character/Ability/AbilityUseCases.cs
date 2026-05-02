using System;
using System.Linq;
using System.Threading.Tasks;
using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.Repository;

namespace STSPlugin.CharacterUseCases;

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
    Task<EquipAbilityResult> ExecuteAsync(Character character, string abilityId, int targetLevel);
}

/// <summary>Implémentation par défaut de <see cref="EquipAbilityUseCase"/>.</summary>
public class DefaultEquipAbilityUseCase : EquipAbilityUseCase
{
    private readonly ICharacterRepository _characterRepository;
    private readonly AbilityRepository    _abilityRepository;

    public DefaultEquipAbilityUseCase(
        ICharacterRepository characterRepository,
        AbilityRepository abilityRepository)
    {
        _characterRepository = characterRepository;
        _abilityRepository   = abilityRepository;
    }

    public async Task<EquipAbilityResult> ExecuteAsync(Character character, string abilityId, int targetLevel)
    {
        var ability = _abilityRepository.GetById(abilityId);
        if (ability is null) return EquipAbilityResult.AbilityNotFound;

        if (ability.Category != AbilityCategory.Weapon &&
            ability.RequiredJobIds != null && ability.RequiredJobIds.Count > 0)
        {
            if (character.JobId == null || !ability.RequiredJobIds.Contains(character.JobId))
                return EquipAbilityResult.AbilityNotFound;
        }

        var rank         = Rank.Get(character.RankKey);
        var currentLevel = character.GetAbilityLevel(abilityId);
        var freePoints   = character.GetFreePointsForAbility(abilityId);

        if (currentLevel >= ability.MaxLevel)
            return EquipAbilityResult.AlreadyMaxLevel;

        if (targetLevel > ability.StartLevel && currentLevel < targetLevel - 1)
            return EquipAbilityResult.PreviousLevelRequired;

        var paidLevel = Math.Max(0, targetLevel - freePoints);

        if (paidLevel > 0 && !rank.AllowsAbilityLevel(targetLevel))
            return EquipAbilityResult.RankTooLow;

        if (targetLevel == 2 && rank.MaxAbilityLv2 != -1)
        {
            if (character.CountAbilitiesAtLevel(2) >= rank.MaxAbilityLv2)
                return EquipAbilityResult.LevelCapReached;
        }
        if (targetLevel == 3 && rank.MaxAbilityLv3 != -1)
        {
            if (character.CountAbilitiesAtLevel(3) >= rank.MaxAbilityLv3)
                return EquipAbilityResult.LevelCapReached;
        }

        var currentPaid    = Math.Max(0, currentLevel - freePoints);
        var targetPaid     = Math.Max(0, targetLevel  - freePoints);
        var additionalCost = targetPaid - currentPaid;

        if (additionalCost > character.RemainingSkillPoints)
            return EquipAbilityResult.NotEnoughPoints;

        var equipped = character.EquippedAbilities.FirstOrDefault(a => a.AbilityId == abilityId);
        if (equipped is null)
            character.EquippedAbilities.Add(new EquippedAbility { AbilityId = abilityId, Level = targetLevel });
        else
            equipped.Level = targetLevel;

        await _characterRepository.SaveAsync(character);
        return EquipAbilityResult.Success;
    }
}

/// <summary>Cas d'usage : retirer une compétence apprise.</summary>
public interface UnequipAbilityUseCase
{
    Task ExecuteAsync(Character character, string abilityId);
}

/// <summary>Implémentation par défaut de <see cref="UnequipAbilityUseCase"/>.</summary>
public class DefaultUnequipAbilityUseCase : UnequipAbilityUseCase
{
    private readonly ICharacterRepository _characterRepository;

    public DefaultUnequipAbilityUseCase(ICharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    public async Task ExecuteAsync(Character character, string abilityId)
    {
        var removed = character.EquippedAbilities.RemoveAll(a => a.AbilityId == abilityId) > 0;
        if (removed) await _characterRepository.SaveAsync(character);
    }
}

/// <summary>Cas d'usage : définir les points de compétence accordés par le MJ.</summary>
public interface SetSkillPointsUseCase
{
    Task ExecuteAsync(Character character, int points);
}

/// <summary>Implémentation par défaut de <see cref="SetSkillPointsUseCase"/>.</summary>
public class DefaultSetSkillPointsUseCase : SetSkillPointsUseCase
{
    private readonly ICharacterRepository _characterRepository;

    public DefaultSetSkillPointsUseCase(ICharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    public async Task ExecuteAsync(Character character, int points)
    {
        character.SkillPoints = Math.Max(0, points);
        await _characterRepository.SaveAsync(character);
    }
}

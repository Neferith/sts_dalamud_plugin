using Sts.Domain;
using Sts.Domain.Character;
using STSPlugin.CharacterUseCases;
using Sts.Domain.DataSource;
using Sts.Domain.Repository;

using Sts.Domain.Character;
using STSPlugin.CharacterUseCases;

namespace STSPlugin;

/// <summary>
/// Contrat de la factory du plugin.
/// Décrit tous les services instanciables depuis le plugin.
/// Les implémentations décident de la stratégie (singleton, transient...).
/// </summary>
public interface IPluginFactory
{
    // --- Moteur ---
    StsEngine MakeEngine();

    // --- DataSource ---
    IDataSource MakeDataSource();

    // --- Repositories ---
    ICharacterRepository MakeCharacterRepository();
    TraitRepository MakeTraitRepository();
    JobRepository MakeJobRepository();
    ActionRepository MakeActionRepository();
    AbilityRepository MakeAbilityRepository();

    // --- Use cases personnages ---
    IGetAllCharactersUseCase MakeGetAllCharacters();
    GetActiveCharacterUseCase MakeGetActiveCharacter();
    ICreateCharacterUseCase MakeCreateCharacter();
    IUpdateCharacterUseCase MakeUpdateCharacter();
    IDeleteCharacterUseCase MakeDeleteCharacter();
    SetActiveCharacterUseCase MakeSetActiveCharacter();

    // --- Use cases traits / job ---
    SetJobUseCase MakeSetJob();
    SetOriginTraitUseCase MakeSetOriginTrait();
    EquipTraitUseCase MakeEquipTrait();
    UnequipTraitUseCase MakeUnequipTrait();

    // --- Use cases actions ---
    GetActionsForCharacterUseCase MakeGetActionsForCharacter();
    CreateCustomActionUseCase MakeCreateCustomAction();
    DeleteCustomActionUseCase MakeDeleteCustomAction();

    // --- Use cases compétences ---
    EquipAbilityUseCase MakeEquipAbility();
    UnequipAbilityUseCase MakeUnequipAbility();
    SetSkillPointsUseCase MakeSetSkillPoints();

    // --- Use cases certifications ---
    AddCertificationUseCase MakeAddCertification();
    RemoveCertificationUseCase MakeRemoveCertification();

    // --- Use cases inventaire ---
    AddInventoryItemUseCase MakeAddInventoryItem();
    RemoveInventoryItemUseCase MakeRemoveInventoryItem();
    SetItemSlotUseCase MakeSetItemSlot();
    ReorderInventoryUseCase MakeReorderInventory();
    SetItemIconUseCase MakeSetItemIcon();
}

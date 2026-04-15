using Sts.Domain;
using STSPlugin.CharacterUseCases;
using STSPlugin.DataSource;
using STSPlugin.Repository;

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
    CharacterRepository MakeCharacterRepository();
    TraitRepository MakeTraitRepository();
    JobRepository MakeJobRepository();
    ActionRepository MakeActionRepository();
    AbilityRepository MakeAbilityRepository();

    // --- Use cases personnages ---
    GetAllCharactersUseCase MakeGetAllCharacters();
    GetActiveCharacterUseCase MakeGetActiveCharacter();
    CreateCharacterUseCase MakeCreateCharacter();
    UpdateCharacterUseCase MakeUpdateCharacter();
    DeleteCharacterUseCase MakeDeleteCharacter();
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

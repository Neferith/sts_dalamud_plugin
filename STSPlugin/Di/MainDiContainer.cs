using System.IO;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Sts.Domain;
using Sts.Domain.UseCases;
using STSPlugin.CharacterUseCases;
using STSPlugin.DataSource;
using STSPlugin.Repository;

namespace STSPlugin;

/// <summary>
/// Implémentation principale de <see cref="IPluginFactory"/>.
/// Tous les services sont des singletons — créés à la première demande et réutilisés ensuite.
/// </summary>
public class MainDiContainer : IPluginFactory
{
    private readonly Configuration _config;
    private readonly string _assemblyDir;
    private readonly string _configDir;
    private readonly IPluginLog _log;

    // --- Singletons ---
    private StsEngine? _engine;
    private IDataSource? _dataSource;
    private LocalJsonDataSource? _local;
    private RemoteJsonDataSource? _remote;
    private CharacterRepository? _characterRepository;
    private TraitRepository? _traitRepository;
    private JobRepository? _jobRepository;
    private ActionRepository? _actionRepository;
    private AbilityRepository? _abilityRepository;

    private GetAllCharactersUseCase? _getAllCharacters;
    private GetActiveCharacterUseCase? _getActiveCharacter;
    private CreateCharacterUseCase? _createCharacter;
    private UpdateCharacterUseCase? _updateCharacter;
    private DeleteCharacterUseCase? _deleteCharacter;
    private SetActiveCharacterUseCase? _setActiveCharacter;

    private SetJobUseCase? _setJob;
    private SetOriginTraitUseCase? _setOriginTrait;
    private EquipTraitUseCase? _equipTrait;
    private UnequipTraitUseCase? _unequipTrait;

    private GetActionsForCharacterUseCase? _getActionsForCharacter;
    private CreateCustomActionUseCase? _createCustomAction;
    private DeleteCustomActionUseCase? _deleteCustomAction;

    private EquipAbilityUseCase? _equipAbility;
    private UnequipAbilityUseCase? _unequipAbility;
    private SetSkillPointsUseCase? _setSkillPoints;

    private AddCertificationUseCase? _addCertification;
    private RemoveCertificationUseCase? _removeCertification;

    private AddInventoryItemUseCase? _addInventoryItem;
    private RemoveInventoryItemUseCase? _removeInventoryItem;
    private SetItemSlotUseCase? _setItemSlot;
    private ReorderInventoryUseCase? _reorderInventory;
    private SetItemIconUseCase? _setItemIcon;

    public MainDiContainer(
        Configuration config,
        IDalamudPluginInterface pluginInterface,
        IPluginLog log)
    {
        _config      = config;
        _assemblyDir = pluginInterface.AssemblyLocation.DirectoryName!;
        _configDir   = pluginInterface.GetPluginConfigDirectory();
        _log         = log;
    }

    // --- Moteur ---

    public StsEngine MakeEngine()
        => _engine ??= StsEngine.CreateDefault();

    // --- DataSource ---

    /*  public IDataSource MakeDataSource()
          => _dataSource ??= new CachedDataSource(
              remote: MakeRemoteDataSource(),
              fallback: MakeLocalDataSource(),
              cacheFilePath: Path.Combine(_configDir, "data.cache.json"),
              log: _log);*/
    public IDataSource MakeDataSource()
          => _dataSource ??= MakeLocalDataSource();

    private LocalJsonDataSource MakeLocalDataSource()
        => _local ??= new LocalJsonDataSource(
            Path.Combine(_assemblyDir, "data.json"));

    private RemoteJsonDataSource MakeRemoteDataSource()
        => _remote ??= new RemoteJsonDataSource(_config.BackendUrl);

    // --- Repositories ---

    public CharacterRepository MakeCharacterRepository()
        => _characterRepository ??= new DefaultCharacterRepository(
            Path.Combine(_configDir, "characters"));

    public TraitRepository MakeTraitRepository()
        => _traitRepository ??= new DefaultTraitRepository(MakeDataSource());

    public JobRepository MakeJobRepository()
        => _jobRepository ??= new DefaultJobRepository(MakeDataSource());

    public ActionRepository MakeActionRepository()
        => _actionRepository ??= new DefaultActionRepository(MakeDataSource());

    public AbilityRepository MakeAbilityRepository()
        => _abilityRepository ??= new DefaultAbilityRepository(MakeDataSource());

    // --- Use cases personnages ---

    public GetAllCharactersUseCase MakeGetAllCharacters()
        => _getAllCharacters ??= new DefaultGetAllCharactersUseCase(MakeCharacterRepository());

    public GetActiveCharacterUseCase MakeGetActiveCharacter()
        => _getActiveCharacter ??= new DefaultGetActiveCharacterUseCase(MakeCharacterRepository(), _config);

    public CreateCharacterUseCase MakeCreateCharacter()
        => _createCharacter ??= new DefaultCreateCharacterUseCase(MakeCharacterRepository());

    public UpdateCharacterUseCase MakeUpdateCharacter()
        => _updateCharacter ??= new DefaultUpdateCharacterUseCase(MakeCharacterRepository());

    public DeleteCharacterUseCase MakeDeleteCharacter()
        => _deleteCharacter ??= new DefaultDeleteCharacterUseCase(MakeCharacterRepository(), _config);

    public SetActiveCharacterUseCase MakeSetActiveCharacter()
        => _setActiveCharacter ??= new DefaultSetActiveCharacterUseCase(
            MakeCharacterRepository(), _config, MakeEngine());

    // --- Use cases traits / job ---

    public SetJobUseCase MakeSetJob()
        => _setJob ??= new DefaultSetJobUseCase(MakeCharacterRepository(), MakeJobRepository());

    public SetOriginTraitUseCase MakeSetOriginTrait()
        => _setOriginTrait ??= new DefaultSetOriginTraitUseCase(MakeCharacterRepository(), MakeTraitRepository());

    public EquipTraitUseCase MakeEquipTrait()
        => _equipTrait ??= new DefaultEquipTraitUseCase(MakeCharacterRepository(), MakeTraitRepository());

    public UnequipTraitUseCase MakeUnequipTrait()
        => _unequipTrait ??= new DefaultUnequipTraitUseCase(MakeCharacterRepository());

    // --- Use cases actions ---

    public GetActionsForCharacterUseCase MakeGetActionsForCharacter()
        => _getActionsForCharacter ??= new DefaultGetActionsForCharacterUseCase(MakeActionRepository());

    public CreateCustomActionUseCase MakeCreateCustomAction()
        => _createCustomAction ??= new DefaultCreateCustomActionUseCase(MakeCharacterRepository());

    public DeleteCustomActionUseCase MakeDeleteCustomAction()
        => _deleteCustomAction ??= new DefaultDeleteCustomActionUseCase(MakeCharacterRepository());

    // --- Use cases compétences ---

    public EquipAbilityUseCase MakeEquipAbility()
        => _equipAbility ??= new DefaultEquipAbilityUseCase(MakeCharacterRepository(), MakeAbilityRepository());

    public UnequipAbilityUseCase MakeUnequipAbility()
        => _unequipAbility ??= new DefaultUnequipAbilityUseCase(MakeCharacterRepository());

    public SetSkillPointsUseCase MakeSetSkillPoints()
        => _setSkillPoints ??= new DefaultSetSkillPointsUseCase(MakeCharacterRepository());

    // --- Use cases certifications ---

    public AddCertificationUseCase MakeAddCertification()
        => _addCertification ??= new DefaultAddCertificationUseCase(MakeCharacterRepository());

    public RemoveCertificationUseCase MakeRemoveCertification()
        => _removeCertification ??= new DefaultRemoveCertificationUseCase(MakeCharacterRepository());

    // --- Use cases inventaire ---

    public AddInventoryItemUseCase MakeAddInventoryItem()
        => _addInventoryItem ??= new DefaultAddInventoryItemUseCase(MakeCharacterRepository());

    public RemoveInventoryItemUseCase MakeRemoveInventoryItem()
        => _removeInventoryItem ??= new DefaultRemoveInventoryItemUseCase(MakeCharacterRepository());

    public SetItemSlotUseCase MakeSetItemSlot()
        => _setItemSlot ??= new DefaultSetItemSlotUseCase(MakeCharacterRepository());

    public ReorderInventoryUseCase MakeReorderInventory()
        => _reorderInventory ??= new DefaultReorderInventoryUseCase(MakeCharacterRepository());

    public SetItemIconUseCase MakeSetItemIcon()
        => _setItemIcon ??= new DefaultSetItemIconUseCase(MakeCharacterRepository());
}

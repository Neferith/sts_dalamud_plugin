using System.IO;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.UseCases;
using STSPlugin.CharacterUseCases;
using Sts.Domain.DataSource;
using Sts.Domain.Repository;
using STSPlugin.Repository;
using STSPlugin.Auth;
using STSPlugin.UseCases.Auth;

namespace STSPlugin;

/// <summary>
/// Implémentation principale de <see cref="IPluginFactory"/>.
/// Tous les services sont des singletons — créés à la première demande et réutilisés ensuite.
///
/// Deux modes de stockage des personnages :
/// - Local (défaut) : <see cref="LocalCharacterRepository"/> — fichiers JSON dans le dossier config
/// - Remote (à venir) : RemoteCharacterRepository — appels vers STS.Api
/// Le mode est sélectionnable depuis les Settings du plugin.
/// </summary>
public class MainDiContainer : IPluginFactory
{
    private readonly Configuration _config;
    private readonly string _assemblyDir;
    private readonly string _configDir;
    private readonly IPluginLog _log;

    private AuthState? _authState;
    private ILoginUseCase? _login;
    private ILogoutUseCase? _logout;
    private IGetTokenUseCase? _getToken;

    // --- Infrastructure ---
    private StsEngine? _engine;
    private IDataSource? _dataSource;
    private ICharacterRepository? _characterRepository;
    private TraitRepository? _traitRepository;
    private JobRepository? _jobRepository;
    private ActionRepository? _actionRepository;
    private AbilityRepository? _abilityRepository;

    // --- Use cases personnages (domain) ---
    private IGetAllCharactersUseCase? _getAllCharacters;
    private ICreateCharacterUseCase? _createCharacter;
    private IUpdateCharacterUseCase? _updateCharacter;
    private IDeleteCharacterUseCase? _deleteCharacter;

    // --- Use cases personnages (plugin-specific, sync) ---
    private GetActiveCharacterUseCase? _getActiveCharacter;
    private SetActiveCharacterUseCase? _setActiveCharacter;

    // --- Use cases traits / job ---
    private SetJobUseCase? _setJob;
    private SetOriginTraitUseCase? _setOriginTrait;
    private EquipTraitUseCase? _equipTrait;
    private UnequipTraitUseCase? _unequipTrait;

    // --- Use cases actions ---
    private GetActionsForCharacterUseCase? _getActionsForCharacter;
    private CreateCustomActionUseCase? _createCustomAction;
    private DeleteCustomActionUseCase? _deleteCustomAction;

    // --- Use cases compétences ---
    private EquipAbilityUseCase? _equipAbility;
    private UnequipAbilityUseCase? _unequipAbility;
    private SetSkillPointsUseCase? _setSkillPoints;

    // --- Use cases certifications ---
    private AddCertificationUseCase? _addCertification;
    private RemoveCertificationUseCase? _removeCertification;

    // --- Use cases inventaire ---
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
        _config = config;
        _assemblyDir = pluginInterface.AssemblyLocation.DirectoryName!;
        _configDir = pluginInterface.GetPluginConfigDirectory();
        _log = log;
    }

    // ── Moteur ────────────────────────────────────────────────────────────────

    public StsEngine MakeEngine()
        => _engine ??= StsEngine.CreateDefault(new DalamudStsLogger(_log));

    // ── DataSource ────────────────────────────────────────────────────────────

    public IDataSource MakeDataSource()
    {
        if (_dataSource != null) return _dataSource;

        var remote = new RemoteJsonDataSource(_config.DataUrl);
        var local = new LocalJsonDataSource(Path.Combine(_assemblyDir, "data.json"));
        var cachePath = Path.Combine(_configDir, "data_cache.json");

        _dataSource = new CachedDataSource(remote, local, cachePath, new DalamudStsLogger(_log));
        return _dataSource;
    }

    /// <summary>
    /// Invalide la datasource et les repositories qui en dépendent.
    /// Les singletons seront recréés au prochain accès avec la config courante.
    /// </summary>
    public void ReloadDataSources()
    {
        _dataSource = null;
        _traitRepository = null;
        _jobRepository = null;
        _actionRepository = null;
        _abilityRepository = null;
    }

    // ── Repository personnages ────────────────────────────────────────────────

    /// <summary>
    /// Retourne le repository de personnages actif selon le mode configuré.
    /// Mode local (défaut) : <see cref="LocalCharacterRepository"/>.
    /// Mode remote (à venir) : RemoteCharacterRepository.
    /// </summary>
    public ICharacterRepository MakeCharacterRepository()
    {
        if (_characterRepository != null) return _characterRepository;

        // TODO : quand le mode remote sera implémenté, brancher ici selon _config.CharacterMode
        // if (_config.CharacterMode == CharacterMode.Remote)
        //     _characterRepository = new RemoteCharacterRepository(_config.BackendUrl, _log);
        // else
        _characterRepository = new LocalCharacterRepository(
            Path.Combine(_configDir, "characters"));

        return _characterRepository;
    }

    /// <summary>
    /// Invalide le repository de personnages et les use cases qui en dépendent.
    /// À appeler depuis les Settings lors du changement de mode Local ↔ Remote.
    /// </summary>
    public void ReloadCharacterRepository()
    {
        _characterRepository = null;
        _getAllCharacters = null;
        _createCharacter = null;
        _updateCharacter = null;
        _deleteCharacter = null;
        _getActiveCharacter = null;
        _setActiveCharacter = null;
    }

    // ── Repositories données de référence ─────────────────────────────────────

    public TraitRepository MakeTraitRepository()
        => _traitRepository ??= new DefaultTraitRepository(MakeDataSource());

    public JobRepository MakeJobRepository()
        => _jobRepository ??= new DefaultJobRepository(MakeDataSource());

    public ActionRepository MakeActionRepository()
        => _actionRepository ??= new DefaultActionRepository(MakeDataSource());

    public AbilityRepository MakeAbilityRepository()
        => _abilityRepository ??= new DefaultAbilityRepository(MakeDataSource());

    // ── Use cases personnages (STS.Domain.Character) ──────────────────────────

    public IGetAllCharactersUseCase MakeGetAllCharacters()
        => _getAllCharacters ??= new GetAllCharactersUseCase(MakeCharacterRepository());

    public ICreateCharacterUseCase MakeCreateCharacter()
        => _createCharacter ??= new CreateCharacterUseCase(MakeCharacterRepository());

    public IUpdateCharacterUseCase MakeUpdateCharacter()
        => _updateCharacter ??= new UpdateCharacterUseCase(MakeCharacterRepository());

    public IDeleteCharacterUseCase MakeDeleteCharacter()
        => _deleteCharacter ??= new DeleteCharacterUseCase(MakeCharacterRepository());

    // ── Use cases personnages (plugin-specific, sync) ─────────────────────────

    public GetActiveCharacterUseCase MakeGetActiveCharacter()
        => _getActiveCharacter ??= new DefaultGetActiveCharacterUseCase(MakeCharacterRepository(), _config);

    public SetActiveCharacterUseCase MakeSetActiveCharacter()
        => _setActiveCharacter ??= new DefaultSetActiveCharacterUseCase(
            MakeCharacterRepository(), _config, MakeEngine());

    // ── Use cases traits / job ────────────────────────────────────────────────

    public SetJobUseCase MakeSetJob()
        => _setJob ??= new DefaultSetJobUseCase(MakeCharacterRepository(), MakeJobRepository());

    public SetOriginTraitUseCase MakeSetOriginTrait()
        => _setOriginTrait ??= new DefaultSetOriginTraitUseCase(MakeCharacterRepository(), MakeTraitRepository());

    public EquipTraitUseCase MakeEquipTrait()
        => _equipTrait ??= new DefaultEquipTraitUseCase(MakeCharacterRepository(), MakeTraitRepository());

    public UnequipTraitUseCase MakeUnequipTrait()
        => _unequipTrait ??= new DefaultUnequipTraitUseCase(MakeCharacterRepository());

    // ── Use cases actions ─────────────────────────────────────────────────────

    public GetActionsForCharacterUseCase MakeGetActionsForCharacter()
        => _getActionsForCharacter ??= new DefaultGetActionsForCharacterUseCase(MakeActionRepository());

    public CreateCustomActionUseCase MakeCreateCustomAction()
        => _createCustomAction ??= new DefaultCreateCustomActionUseCase(MakeCharacterRepository());

    public DeleteCustomActionUseCase MakeDeleteCustomAction()
        => _deleteCustomAction ??= new DefaultDeleteCustomActionUseCase(MakeCharacterRepository());

    // ── Use cases compétences ─────────────────────────────────────────────────

    public EquipAbilityUseCase MakeEquipAbility()
        => _equipAbility ??= new DefaultEquipAbilityUseCase(MakeCharacterRepository(), MakeAbilityRepository());

    public UnequipAbilityUseCase MakeUnequipAbility()
        => _unequipAbility ??= new DefaultUnequipAbilityUseCase(MakeCharacterRepository());

    public SetSkillPointsUseCase MakeSetSkillPoints()
        => _setSkillPoints ??= new DefaultSetSkillPointsUseCase(MakeCharacterRepository());

    // ── Use cases certifications ──────────────────────────────────────────────

    public AddCertificationUseCase MakeAddCertification()
        => _addCertification ??= new DefaultAddCertificationUseCase(MakeCharacterRepository());

    public RemoveCertificationUseCase MakeRemoveCertification()
        => _removeCertification ??= new DefaultRemoveCertificationUseCase(MakeCharacterRepository());

    // ── Use cases inventaire ──────────────────────────────────────────────────

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


    /// <summary>État d'authentification partagé — singleton.</summary>
    public AuthState MakeAuthState()
        => _authState ??= new AuthState();

    /// <summary>Cas d'usage : connexion joueur.</summary>
    public ILoginUseCase MakeLogin()
        => _login ??= new LoginUseCase(MakeAuthState(), _config);

    /// <summary>Cas d'usage : déconnexion joueur.</summary>
    public ILogoutUseCase MakeLogout()
        => _logout ??= new LogoutUseCase(MakeAuthState());

    /// <summary>Cas d'usage : obtenir un token valide (renouvelle si expiré).</summary>
    public IGetTokenUseCase MakeGetToken()
        => _getToken ??= new GetTokenUseCase(MakeAuthState(), _config, MakeLogin());
}

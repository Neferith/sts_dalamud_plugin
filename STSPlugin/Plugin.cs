using Dalamud.Game.Chat;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.Repository;
using STSPlugin.Auth;
using STSPlugin.CharacterUseCases;
using STSPlugin.ConfigDomain;
using STSPlugin.Repository;
using STSPlugin.UseCases.Auth;
using STSPlugin.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace STSPlugin;

public sealed class Plugin : IDalamudPlugin
{
    private const string CmdMain = "/sts";

    private static readonly Regex RandomRegex = new(@"(\d+)[^\d]*$", RegexOptions.Compiled);

    [PluginService] public static IFramework Framework { get; private set; }

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;

    public Configuration Configuration { get; init; }
    public StsEngine Engine { get; init; }

    public CharacterStore CharacterStore { get; init; }

    private readonly MainDiContainer _factory;
    public ICharacterRepository CharacterRepository { get; init; }
    public TraitRepository TraitRepository { get; private set; }
    public JobRepository JobRepository { get; private set; }
    public ActionRepository ActionRepository { get; private set; }
    public AbilityRepository AbilityRepository { get; private set; }

    // --- use cases auth ---
    public AuthState AuthState { get; init; }
    public ILoginUseCase Login { get; init; }
    public ILogoutUseCase Logout { get; init; }
    public IGetTokenUseCase GetToken { get; init; }

    // --- use cases personnages ---
    public IGetAllCharactersUseCase GetAllCharacters { get; private set; }
    public GetActiveCharacterUseCase GetActiveCharacter { get; private set; }
    public ICreateCharacterUseCase CreateCharacter { get; private set; }
    public IUpdateCharacterUseCase UpdateCharacter { get; private set; }
    public IDeleteCharacterUseCase DeleteCharacter { get; private set; }
    public SetActiveCharacterUseCase SetActiveCharacter { get; private set; }

    // --- use cases traits / job ---
    public SetJobUseCase SetJob { get; private set; }
    public SetOriginTraitUseCase SetOriginTrait { get; private set; }
    public EquipTraitUseCase EquipTrait { get; private set; }
    public UnequipTraitUseCase UnequipTrait { get; private set; }

    // --- use cases actions ---
    public GetActionsForCharacterUseCase GetActionsForCharacter { get; private set; }
    public CreateCustomActionUseCase CreateCustomAction { get; private set; }
    public DeleteCustomActionUseCase DeleteCustomAction { get; private set; }

    // --- use cases compétences ---
    public EquipAbilityUseCase EquipAbility { get; private set; }
    public UnequipAbilityUseCase UnequipAbility { get; private set; }
    public SetSkillPointsUseCase SetSkillPoints { get; private set; }

    // --- use cases certifications ---
    public AddCertificationUseCase AddCertification { get; private set; }
    public RemoveCertificationUseCase RemoveCertification { get; private set; }

    // --- use cases inventaire ---
    public AddInventoryItemUseCase AddInventoryItem { get; private set; }
    public RemoveInventoryItemUseCase RemoveInventoryItem { get; private set; }
    public SetItemSlotUseCase SetItemSlot { get; private set; }
    public ReorderInventoryUseCase ReorderInventory { get; private set; }
    public SetItemIconUseCase SetItemIcon { get; private set; }

    private readonly WindowSystem windowSystem = new("STSPlugin");
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private QuickbarWindow? quickbarWindow;

    private readonly Dictionary<Guid, CharacterWindow> _characterWindows = new();

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        CharacterStore = new CharacterStore();

        // --- Container ---
        IPluginFactory factory = new MainDiContainer(CharacterStore, Configuration, PluginInterface, Log);
        _factory = (MainDiContainer)factory;

        // --- Engine ---
        Engine = factory.MakeEngine();

        // --- Repositories ---
        CharacterRepository = factory.MakeCharacterRepository();
        TraitRepository = factory.MakeTraitRepository();
        JobRepository = factory.MakeJobRepository();
        ActionRepository = factory.MakeActionRepository();
        AbilityRepository = factory.MakeAbilityRepository();

        // --- Use cases auth ---
        AuthState = _factory.MakeAuthState();
        Login = _factory.MakeLogin();
        Logout = _factory.MakeLogout();
        GetToken = _factory.MakeGetToken();

        // --- Use cases personnages ---
        GetAllCharacters = factory.MakeGetAllCharacters();
        GetActiveCharacter = factory.MakeGetActiveCharacter();
        CreateCharacter = factory.MakeCreateCharacter();
        UpdateCharacter = factory.MakeUpdateCharacter();
        DeleteCharacter = factory.MakeDeleteCharacter();
        SetActiveCharacter = factory.MakeSetActiveCharacter();

        // --- Use cases traits / job ---
        SetJob = factory.MakeSetJob();
        SetOriginTrait = factory.MakeSetOriginTrait();
        EquipTrait = factory.MakeEquipTrait();
        UnequipTrait = factory.MakeUnequipTrait();

        // --- Use cases actions ---
        GetActionsForCharacter = factory.MakeGetActionsForCharacter();
        CreateCustomAction = factory.MakeCreateCustomAction();
        DeleteCustomAction = factory.MakeDeleteCustomAction();

        // --- Use cases compétences ---
        EquipAbility = factory.MakeEquipAbility();
        UnequipAbility = factory.MakeUnequipAbility();
        SetSkillPoints = factory.MakeSetSkillPoints();

        // --- Use cases certifications ---
        AddCertification = factory.MakeAddCertification();
        RemoveCertification = factory.MakeRemoveCertification();

        // --- Use cases inventaire ---
        AddInventoryItem = factory.MakeAddInventoryItem();
        RemoveInventoryItem = factory.MakeRemoveInventoryItem();
        SetItemSlot = factory.MakeSetItemSlot();
        ReorderInventory = factory.MakeReorderInventory();
        SetItemIcon = factory.MakeSetItemIcon();

        // --- Rang par défaut depuis la config en attendant le fetch initial ---
        if (Enum.TryParse<RankKey>(Configuration.LastRank, out var rankKey))
            Engine.ChangeRank(rankKey);

        // --- Abonnements store ---
        // OnActiveChanged : synchronise engine + UI quand le personnage actif change
        _factory.CharacterStore.OnActiveChanged += () =>
        {
            RefreshEquippedTraits(_factory.CharacterStore.Active);
           // mainWindow?.TriggerRefresh();
          //  quickbarWindow?.TriggerRefresh();
        };



        // --- Auth : abonnement changement d'état + login automatique ---
        AuthState.OnAuthChanged += () =>
        {
            Log.Debug("[STS] AuthState changé");
            _factory.ReloadCharacterRepository();

            // Réassigner tous les use cases depuis le nouveau repository
            GetAllCharacters = _factory.MakeGetAllCharacters();
            GetActiveCharacter = _factory.MakeGetActiveCharacter();
            CreateCharacter = _factory.MakeCreateCharacter();
            UpdateCharacter = _factory.MakeUpdateCharacter();
            DeleteCharacter = _factory.MakeDeleteCharacter();
            SetActiveCharacter = _factory.MakeSetActiveCharacter();
            SetJob = _factory.MakeSetJob();
            SetOriginTrait = _factory.MakeSetOriginTrait();
            EquipTrait = _factory.MakeEquipTrait();
            UnequipTrait = _factory.MakeUnequipTrait();
            GetActionsForCharacter = _factory.MakeGetActionsForCharacter();
            CreateCustomAction = _factory.MakeCreateCustomAction();
            DeleteCustomAction = _factory.MakeDeleteCustomAction();
            EquipAbility = _factory.MakeEquipAbility();
            UnequipAbility = _factory.MakeUnequipAbility();
            SetSkillPoints = _factory.MakeSetSkillPoints();
            AddCertification = _factory.MakeAddCertification();
            RemoveCertification = _factory.MakeRemoveCertification();
            AddInventoryItem = _factory.MakeAddInventoryItem();
            RemoveInventoryItem = _factory.MakeRemoveInventoryItem();
            SetItemSlot = _factory.MakeSetItemSlot();
            ReorderInventory = _factory.MakeReorderInventory();
            SetItemIcon = _factory.MakeSetItemIcon();

            // Fetch → decorator peuple le store → OnActiveChanged + OnListChanged → UI
            var currentActiveId = Configuration.ActiveCharacterId;
            _ = Task.Run(async () =>
            {
                try
                {
                    var all = await GetAllCharacters.ExecuteAsync();
                    // Si le personnage actif a disparu du nouveau set, on le réinitialise
                    if (currentActiveId.HasValue && !all.Any(c => c.Id == currentActiveId.Value))
                        SetActiveCharacter.Execute(null);
                }
                catch (Exception ex)
                {
                    Log.Warning("[STS] OnAuthChanged — rechargement échoué : {0}", ex.Message);
                }
            });
        };

        // --- Auto-login ---
        if (!string.IsNullOrWhiteSpace(Configuration.PlayerUsername) &&
            !string.IsNullOrWhiteSpace(Configuration.PlayerPassword))
        {
            _ = Task.Run(() => Login.ExecuteAsync(
                Configuration.PlayerUsername,
                Configuration.PlayerPassword));
        }

        // --- Windows ---
        mainWindow = new MainWindow(this);
        configWindow = new ConfigWindow(this);

        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(configWindow);

        // --- Fetch initial — peuple le store → OnActiveChanged → RefreshEquippedTraits ---
        _ = Task.Run(() => GetAllCharacters.ExecuteAsync());

        CommandManager.AddHandler(CmdMain, new CommandInfo(OnCommand)
        {
            HelpMessage = "Ouvre/ferme l'interface STS. \"/sts roll\" lance les dés. \"/sts roll <id>\" lance une action. \"/sts quickbar\" ouvre la barre."
        });

        ChatGui.ChatMessage += OnChatMessage;
        ChatGui.ChatMessageUnhandled += OnChatMessageUnhandled;

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        foreach (var w in _characterWindows.Values)
            windowSystem.RemoveWindow(w);
        _characterWindows.Clear();

        CommandManager.RemoveHandler(CmdMain);
        windowSystem.RemoveAllWindows();
    }

    public void ReloadDataSources()
    {
        Task.Run(() =>
        {
            try
            {
                _factory.ReloadDataSources();

                var dataSource = _factory.MakeDataSource();
                dataSource.Load();

                TraitRepository = _factory.MakeTraitRepository();
                JobRepository = _factory.MakeJobRepository();
                ActionRepository = _factory.MakeActionRepository();
                AbilityRepository = _factory.MakeAbilityRepository();

                RefreshEquippedTraits();

                Log.Information("[STS] Données rechargées — mode : {0}, url : {1}",
                    Configuration.DataSourceMode, Configuration.ApiBaseUrl);
            }
            catch (Exception ex)
            {
                Log.Error("[STS] Erreur au rechargement des données : {0}", ex.Message);
            }
        });
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "roll":
            case "r":
                StartRoll(action: null);
                break;
            case string s when s.StartsWith("roll ") || s.StartsWith("r "):
                {
                    var actionId = args.Trim().Split(' ', 2)[1].Trim();
                    var active = GetActiveCharacter.Execute();
                    var action = active != null
                        ? GetActionsForCharacter.GetAll(active).FirstOrDefault(a => a.Id == actionId)
                        : null;

                    if (action is null) PrintInfo($"Action inconnue : {actionId}");
                    else StartRoll(action);
                    break;
                }
            case "reroll":
            case "rr":
                StartReroll();
                break;
            case "quickbar":
            case "qb":
                ToggleQuickbar();
                break;
            case "config":
                configWindow.Toggle();
                break;
            default:
                ToggleMainUi();
                break;
        }
    }

    public void OpenCharacterWindow(Character character)
    {
        if (_characterWindows.TryGetValue(character.Id, out var existing))
        {
            existing.IsOpen = true;
            return;
        }

        var window = new CharacterWindow(this, character);
        _characterWindows[character.Id] = window;
        windowSystem.AddWindow(window);
        window.IsOpen = true;
    }

    public void RefreshCharacterWindows(IReadOnlyList<Character> freshCharacters)
    {
        foreach (var (id, window) in _characterWindows)
        {
            var fresh = freshCharacters.FirstOrDefault(c => c.Id == id);
            if (fresh is not null)
                window.UpdateCharacter(fresh);
        }
    }

    public void RefreshEquippedTraits(Character? character = null)
    {
        var active = character ?? GetActiveCharacter.Execute();

        Log.Debug("[STS] RefreshEquippedTraits — character param: {0}, resolved: {1}",
            character?.Name ?? "null",
            active?.Name ?? "null");

        if (active is null)
        {
            Log.Debug("[STS] RefreshEquippedTraits — aucun personnage actif, reset traits");
            Engine.SetEquippedTraits([]);
            return;
        }

        Log.Debug("[STS] RefreshEquippedTraits — rang: {0}, traits équipés: {1}, trait origine: {2}",
            active.RankKey,
            active.EquippedTraitIds.Count,
            active.OriginTraitId ?? "null");

        Engine.ChangeRank(active.RankKey);

        var traits = active.EquippedTraitIds
            .Select(id => TraitRepository.GetById(id))
            .Where(t => t != null)
            .Cast<Trait>()
            .ToList();

        if (active.OriginTraitId is { } originId
            && TraitRepository.GetById(originId) is { } originTrait)
        {
            traits.Add(originTrait);
        }

        Log.Debug("[STS] RefreshEquippedTraits — {0} trait(s) résolus: [{1}]",
            traits.Count,
            string.Join(", ", traits.Select(t => t.Name)));

        Engine.SetEquippedTraits(traits);

        Log.Debug("[STS] RefreshEquippedTraits — terminé, rang engine: {0}, palier: {1}",
            Engine.CurrentRank.Label,
            Engine.EffectivePalier);
    }

    // ------------------------------------------------------------------ Roll

    public void StartRoll(RollAction? action)
    {
        mainWindow.IsOpen = true;

        Engine.PalierOverride = null;
        if (action != null)
        {
            var active = GetActiveCharacter.Execute();
            if (active != null)
                Engine.PalierOverride = EvaluateRequirements(action, active);
        }

        if (Configuration.RollSource == RollSource.GameRandom)
        {
            if (action != null) Engine.BeginRoll(action);
            else Engine.BeginRoll();
            SendRaw("/random");
        }
        else
        {
            if (action != null) Engine.Roll(action);
            else Engine.Roll();
            OnRollComplete();
        }
    }

    private int? EvaluateRequirements(RollAction action, Character character)
    {
        foreach (var req in action.Requirements)
        {
            switch (req)
            {
                case ActionRequirementType.Weapon:
                    var equipped = character.EquippedWeapons.ToList();
                    if (equipped.Count == 0 || equipped.All(w => character.IsWeaponUnmastered(w)))
                        return 8;
                    break;
            }
        }
        return null;
    }

    public void StartReroll()
    {
        if (!Engine.HasRolled) { PrintInfo("Aucun jet en cours."); return; }
        if (Engine.RerollsLeft <= 0) { PrintInfo("Plus de rerolls disponibles."); return; }

        if (Configuration.RollSource == RollSource.GameRandom)
        {
            Engine.BeginReroll();
            SendRaw("/random");
        }
        else
        {
            Engine.Reroll();
            OnRollComplete();
        }
    }

    // ------------------------------------------------------------------ Interception /random

    private void OnChatMessage(IHandleableChatMessage message)
    {
        Log.Debug($"[STS] ChatMessage — logKind: {(int)message.LogKind}, sender: '{message.Sender.TextValue}', message: '{message.Message.TextValue}'");

        if (Configuration.RollSource != RollSource.GameRandom)
        {
            Log.Debug($"[STS] Skip — RollSource={Configuration.RollSource}");
            return;
        }
        if (Engine.State != EngineState.WaitingDice)
        {
            Log.Debug($"[STS] Skip — EngineState={Engine.State}");
            return;
        }
        if ((int)message.LogKind != 74)
        {
            Log.Debug($"[STS] Skip — LogKind={message.LogKind} != 74");
            return;
        }

        var text = message.Message.TextValue;
        var match = RandomRegex.Match(text);
        Log.Debug($"[STS] Regex sur '{text}' — match: {match.Success}");

        if (!match.Success) return;
        if (!int.TryParse(match.Groups[1].Value, out var value))
        {
            Log.Debug($"[STS] Parse échoué sur '{match.Groups[1].Value}'");
            return;
        }
        Log.Debug($"[STS] Valeur parsée: {value}");
        if (value < 0 || value > 999)
        {
            Log.Debug($"[STS] Skip — valeur hors range: {value}");
            return;
        }

        Log.Debug($"[STS] /random reçu : {value:D3}");

        if (Engine.ReceiveRandom(value))
            Plugin.Framework.RunOnTick(OnRollComplete);
    }

    private void OnChatMessageUnhandled(IChatMessage message)
    {
        Log.Debug($"[STS] Unhandled reçu — logKind: {(int)message.LogKind}");
    }

    // ------------------------------------------------------------------ Résultat

    private void OnRollComplete()
    {
        PrintStyledLocal();
        if (Configuration.EchoToChat)
            SendToChannel(Configuration.ChatChannel, BuildPlainMessage());
    }

    // ------------------------------------------------------------------ Affichage

    private void PrintStyledLocal()
    {
        if (Engine.LastResult is not { } result) return;

        var rank = Engine.CurrentRank;
        var localPlayer = Plugin.ObjectTable.LocalPlayer;
        var player = localPlayer?.Name.TextValue ?? "???";

        const ushort ColGreen = 43;
        const ushort ColGrey = 4;
        const ushort ColRed = 17;
        const ushort ColGold = 559;
        const ushort ColWhite = 1;

        var sb = new SeStringBuilder();

        sb.AddUiForeground(ColWhite);
        sb.AddText(player);
        sb.AddUiForegroundOff();
        sb.AddText(" ");

        sb.AddUiForeground(ColGold);
        var actionLabel = result.Action != null ? $" — {result.Action.Name}" : "";
        sb.AddText($"[{rank.Label}{actionLabel} · palier {result.Palier}+]");
        sb.AddUiForegroundOff();
        sb.AddText("  ");

        for (var i = 0; i < result.Chosen.Values.Length; i++)
        {
            var val = result.Chosen.Values[i];
            var suc = val >= result.Palier;
            if (i > 0) sb.AddText(" · ");
            sb.AddUiForeground(suc ? ColGreen : ColGrey);
            sb.AddText(DiceSet.Display(val));
            sb.AddUiForegroundOff();
        }

        sb.AddText("  →  ");

        if (result.TraitEffects.BonusSuccesses > 0 || result.TraitEffects.MalusSuccesses > 0)
        {
            sb.AddUiForeground(ColGrey);
            sb.AddText($"({result.RawSuccesses} dés");
            if (result.TraitEffects.BonusSuccesses > 0)
                sb.AddText($" +{result.TraitEffects.BonusSuccesses} traits ({string.Join(", ", result.TraitEffects.BonusTraitNames)})");
            if (result.TraitEffects.MalusSuccesses > 0)
                sb.AddText($" -{result.TraitEffects.MalusSuccesses} malus ({string.Join(", ", result.TraitEffects.MalusTraitNames)})");
            sb.AddText(")  →  ");
            sb.AddUiForegroundOff();
        }

        ushort resCol = result.Successes == 0 ? ColRed : result.Successes >= 2 ? ColGreen : ColWhite;
        sb.AddUiForeground(resCol);
        sb.AddText(result.Successes == 0 ? "Échec total" : result.Successes == 1 ? "1 succès" : $"{result.Successes} succès");
        sb.AddUiForegroundOff();

        var rrLeft = Engine.RerollsLeft;
        if (rrLeft > 0)
        {
            sb.AddUiForeground(ColGrey);
            sb.AddText($"  ({rrLeft} reroll{(rrLeft > 1 ? "s" : "")} restant{(rrLeft > 1 ? "s" : "")})");
            sb.AddUiForegroundOff();
        }

        ChatGui.Print(new XivChatEntry
        {
            Type = XivChatType.SystemMessage,
            Name = SeString.Empty,
            Message = sb.Build(),
        });
    }

    private string BuildPlainMessage()
    {
        if (Engine.LastResult is not { } result) return string.Empty;

        var rank = Engine.CurrentRank;
        var dice = $"({result.Chosen.ToDisplayString()})";
        var actionPart = result.Action != null ? $" [{result.Action.Name}]" : "";
        var res = result.Successes == 0 ? "Échec total"
                       : result.Successes == 1 ? "1 succès"
                       : $"{result.Successes} succès";
        var mod = Engine.Modifier != 0
                       ? $" modif {(Engine.Modifier > 0 ? "+" : "")}{Engine.Modifier}"
                       : "";

        string traitPart = "";
        if (result.TraitEffects.BonusSuccesses > 0 || result.TraitEffects.MalusSuccesses > 0)
        {
            var parts = new List<string>();
            if (result.TraitEffects.BonusSuccesses > 0)
            {
                var names = string.Join(", ", result.TraitEffects.BonusTraitNames);
                parts.Add($"+{result.TraitEffects.BonusSuccesses} ({names})");
            }
            if (result.TraitEffects.MalusSuccesses > 0)
            {
                var names = string.Join(", ", result.TraitEffects.MalusTraitNames);
                parts.Add($"-{result.TraitEffects.MalusSuccesses} ({names})");
            }
            traitPart = $" · {string.Join(" ", parts)}";
        }

        return $"[STS] {rank.Label}{actionPart}{mod} · {dice} · palier {result.Palier}+{traitPart} → {res}";
    }

    private void PrintInfo(string msg)
    {
        ChatGui.Print(new XivChatEntry
        {
            Type = XivChatType.SystemMessage,
            Name = SeString.Empty,
            Message = new SeStringBuilder().AddText($"[STS] {msg}").Build(),
        });
    }

    // ------------------------------------------------------------------ Quickbar

    public void ToggleQuickbar()
    {
        if (quickbarWindow is null)
        {
            quickbarWindow = new QuickbarWindow(this);
            windowSystem.AddWindow(quickbarWindow);
        }
        quickbarWindow.Toggle();
    }

    // ------------------------------------------------------------------ Chat natif

    private static unsafe void SendToChannel(string channel, string message)
    {
        var uiModule = UIModule.Instance();
        if (uiModule == null) { Log.Warning("[STS] UIModule introuvable."); return; }

        var fullMessage = string.IsNullOrEmpty(channel) ? message : $"/{channel} {message}";
        var bytes = Encoding.UTF8.GetBytes(fullMessage);
        var utf8String = new Utf8String();
        fixed (byte* ptr = bytes) utf8String.SetString(ptr);
        uiModule->ProcessChatBoxEntry(&utf8String);
        utf8String.Dtor();
    }

    private static unsafe void SendRaw(string command)
    {
        var uiModule = UIModule.Instance();
        if (uiModule == null) return;

        var bytes = Encoding.UTF8.GetBytes(command);
        var utf8String = new Utf8String();
        fixed (byte* ptr = bytes) utf8String.SetString(ptr);
        uiModule->ProcessChatBoxEntry(&utf8String);
        utf8String.Dtor();
    }

    // ------------------------------------------------------------------ UI

    private void DrawUi() => windowSystem.Draw();
    public void ToggleMainUi() => mainWindow.Toggle();
    public void ToggleConfigUi() => configWindow.Toggle();

    public void SaveRank(RankKey rankKey)
    {
        Configuration.LastRank = rankKey.ToString();
        Configuration.Save();
    }
}

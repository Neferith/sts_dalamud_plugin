using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using STSPlugin.DataSource;
using Sts.Domain;
using STSPlugin.Repository;
using STSPlugin.CharacterUseCases;
using Sts.Domain.UseCases;
using STSPlugin.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using STSPlugin.ConfigDomain;

namespace STSPlugin;

public sealed class Plugin : IDalamudPlugin
{
    private const string CmdMain = "/sts";

    private static readonly Regex RandomRegex = new(@"(\d+)[^\d]*$", RegexOptions.Compiled);

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;

    public Configuration Configuration { get; init; }
    public StsEngine Engine { get; init; }
    public CharacterRepository CharacterRepository { get; init; }
    public TraitRepository TraitRepository { get; init; }
    public JobRepository JobRepository { get; init; }
    public ActionRepository ActionRepository { get; init; }
    public AbilityRepository AbilityRepository { get; init; }

    // --- use cases personnages ---
    public GetAllCharactersUseCase GetAllCharacters { get; init; }
    public GetActiveCharacterUseCase GetActiveCharacter { get; init; }
    public CreateCharacterUseCase CreateCharacter { get; init; }
    public UpdateCharacterUseCase UpdateCharacter { get; init; }
    public DeleteCharacterUseCase DeleteCharacter { get; init; }
    public SetActiveCharacterUseCase SetActiveCharacter { get; init; }

    // --- use cases traits / job ---
    public SetJobUseCase SetJob { get; init; }
    public SetOriginTraitUseCase SetOriginTrait { get; init; }
    public EquipTraitUseCase EquipTrait { get; init; }
    public UnequipTraitUseCase UnequipTrait { get; init; }

    // --- use cases actions ---
    public GetActionsForCharacterUseCase GetActionsForCharacter { get; init; }
    public CreateCustomActionUseCase CreateCustomAction { get; init; }
    public DeleteCustomActionUseCase DeleteCustomAction { get; init; }

    // --- use cases compétences ---
    public EquipAbilityUseCase EquipAbility { get; init; }
    public UnequipAbilityUseCase UnequipAbility { get; init; }
    public SetSkillPointsUseCase SetSkillPoints { get; init; }

    // --- use cases certifications ---
    public AddCertificationUseCase AddCertification { get; init; }
    public RemoveCertificationUseCase RemoveCertification { get; init; }

    // --- use cases inventaire ---
    public AddInventoryItemUseCase AddInventoryItem { get; init; }
    public RemoveInventoryItemUseCase RemoveInventoryItem { get; init; }
    public SetItemSlotUseCase SetItemSlot { get; init; }
    public ReorderInventoryUseCase ReorderInventory { get; init; }
    public SetItemIconUseCase SetItemIcon { get; init; }

    private readonly WindowSystem windowSystem = new("STSPlugin");
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private QuickbarWindow? quickbarWindow;

    private readonly Dictionary<Guid, CharacterWindow> _characterWindows = new();

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // --- Container ---
        IPluginFactory factory = new MainDiContainer(Configuration, PluginInterface, Log);

        // --- Engine ---
        Engine = factory.MakeEngine();

        // --- DataSource ---
        // var dataPath = Path.Combine(PluginInterface.AssemblyLocation.DirectoryName!, "data.json");
        // var dataSource = new LocalJsonDataSource(dataPath);

        // --- Repositories ---
        var charactersDir = //Path.Combine(PluginInterface.GetPluginConfigDirectory(), "characters");
        CharacterRepository = factory.MakeCharacterRepository();//new DefaultCharacterRepository(charactersDir);
        TraitRepository = factory.MakeTraitRepository();//new DefaultTraitRepository(dataSource);
        JobRepository = factory.MakeJobRepository();//new DefaultJobRepository(dataSource);
        ActionRepository = factory.MakeActionRepository();//new DefaultActionRepository(dataSource);
        AbilityRepository = factory.MakeAbilityRepository();//new DefaultAbilityRepository(dataSource);

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

        // --- Appliquer le personnage actif au démarrage ---
        var active = GetActiveCharacter.Execute();
        if (active != null)
        {
            Engine.ChangeRank(active.RankKey);
            RefreshEquippedTraits(active);
        }
        else if (Enum.TryParse<RankKey>(Configuration.LastRank, out var rankKey))
        {
            Engine.ChangeRank(rankKey);
        }

        // --- Windows ---
        mainWindow = new MainWindow(this);
        configWindow = new ConfigWindow(this);

        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(configWindow);

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
        ChatGui.ChatMessage -= OnChatMessage;
        ChatGui.ChatMessageUnhandled -= OnChatMessageUnhandled;

        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        foreach (var w in _characterWindows.Values)
            windowSystem.RemoveWindow(w);
        _characterWindows.Clear();

        CommandManager.RemoveHandler(CmdMain);
        windowSystem.RemoveAllWindows();
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

    /// <summary>
    /// Met à jour les traits équipés dans l'engine quand le personnage actif change.
    /// À appeler après tout changement de personnage actif ou de traits équipés.
    /// </summary>
    public void RefreshEquippedTraits(Character? character = null)
    {
        var active = character ?? GetActiveCharacter.Execute();
        if (active is null)
        {
            Engine.SetEquippedTraits([]);
            return;
        }

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

        Engine.SetEquippedTraits(traits);
    }

    // ------------------------------------------------------------------ Roll

    public void StartRoll(RollAction? action)
    {
        mainWindow.IsOpen = true;

        // Évaluer les prérequis de l'action avant le jet
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

    /// <summary>
    /// Évalue les prérequis d'une action et retourne un palier forcé si une règle s'applique.
    /// Null = pas de forçage.
    /// </summary>
    private int? EvaluateRequirements(RollAction action, Character character)
    {
        foreach (var req in action.Requirements)
        {
            switch (req)
            {
                case ActionRequirementType.Weapon:
                    var equipped = character.EquippedWeapons.ToList();
                    // Pas d'arme équipée ou toutes non maîtrisées → palier 8
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

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        Log.Debug($"[STS] Chat reçu — type: {(int)type} ({type}), sender: '{sender.TextValue}', message: '{message.TextValue}'");
    }

    private void OnChatMessageUnhandled(XivChatType type, int timestamp, SeString sender, SeString message)
    {
        if (Configuration.RollSource != RollSource.GameRandom) return;
        if (Engine.State != EngineState.WaitingDice) return;
        if ((int)type != 2122) return;

        var text = message.TextValue;
        var match = RandomRegex.Match(text);

        Log.Debug($"[STS] Unhandled 2122 : '{text}' | match : {match.Success}");

        if (!match.Success) return;
        if (!int.TryParse(match.Groups[1].Value, out var value)) return;
        if (value < 0 || value > 999) return;

        Log.Debug($"[STS] /random reçu : {value:D3}");
        if (Engine.ReceiveRandom(value))
            OnRollComplete();
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
        var player = ClientState.LocalPlayer?.Name.ToString() ?? "???";

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
                sb.AddText($" +{result.TraitEffects.BonusSuccesses} traits");
            if (result.TraitEffects.MalusSuccesses > 0)
                sb.AddText($" -{result.TraitEffects.MalusSuccesses} malus");
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

        var bytes = Encoding.UTF8.GetBytes($"/{channel} {message}");
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

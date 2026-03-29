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
using STSPlugin.Domain;
using STSPlugin.Repository;
using STSPlugin.UseCases;
using STSPlugin.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

    public Configuration Configuration { get; init; }
    public StsEngine Engine { get; init; }
    public CharacterRepository CharacterRepository { get; init; }
    public TraitRepository TraitRepository { get; init; }
    public JobRepository JobRepository { get; init; }
    public ActionRepository ActionRepository { get; init; }

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

    private readonly WindowSystem windowSystem = new("STSPlugin");
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private QuickbarWindow? quickbarWindow;

    private readonly Dictionary<Guid, CharacterWindow> _characterWindows = new();

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // --- Engine ---
        Engine = new StsEngine(
            new DefaultComputePalierUseCase(),
            new DefaultResolveDiceSetUseCase(),
            new DefaultPickDiceSetUseCase(),
            new DefaultCheckRerollUseCase()
        );

        // --- DataSource ---
        var dataPath = Path.Combine(PluginInterface.AssemblyLocation.DirectoryName!, "data.json");
        var dataSource = new LocalJsonDataSource(dataPath);

        // --- Repositories ---
        var charactersDir = Path.Combine(PluginInterface.GetPluginConfigDirectory(), "characters");
        CharacterRepository = new DefaultCharacterRepository(charactersDir);
        TraitRepository = new DefaultTraitRepository(dataSource);
        JobRepository = new DefaultJobRepository(dataSource);
        ActionRepository = new DefaultActionRepository(dataSource);

        // --- Use cases personnages ---
        GetAllCharacters = new DefaultGetAllCharactersUseCase(CharacterRepository);
        GetActiveCharacter = new DefaultGetActiveCharacterUseCase(CharacterRepository, Configuration);
        CreateCharacter = new DefaultCreateCharacterUseCase(CharacterRepository);
        UpdateCharacter = new DefaultUpdateCharacterUseCase(CharacterRepository);
        DeleteCharacter = new DefaultDeleteCharacterUseCase(CharacterRepository, Configuration);
        SetActiveCharacter = new DefaultSetActiveCharacterUseCase(CharacterRepository, Configuration, Engine);

        // --- Use cases traits / job ---
        SetJob = new DefaultSetJobUseCase(CharacterRepository, JobRepository);
        SetOriginTrait = new DefaultSetOriginTraitUseCase(CharacterRepository, TraitRepository);
        EquipTrait = new DefaultEquipTraitUseCase(CharacterRepository, TraitRepository);
        UnequipTrait = new DefaultUnequipTraitUseCase(CharacterRepository);

        // --- Use cases actions ---
        GetActionsForCharacter = new DefaultGetActionsForCharacterUseCase(ActionRepository);
        CreateCustomAction = new DefaultCreateCustomActionUseCase(CharacterRepository);
        DeleteCustomAction = new DefaultDeleteCustomActionUseCase(CharacterRepository);

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
            HelpMessage = "Ouvre/ferme l'interface STS. \"/sts roll\" lance les dés. \"/sts quickbar\" ouvre la barre de raccourcis."
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

        // Inclure le trait d'origine s'il est équipé
        if (active.OriginTraitId is { } originId
            && TraitRepository.GetById(originId) is { } originTrait)
        {
            traits.Add(originTrait);
        }

        Engine.SetEquippedTraits(traits);
    }

    // ------------------------------------------------------------------ Roll

    /// <summary>Lance un jet avec une action optionnelle.</summary>
    public void StartRoll(RollAction? action)
    {
        mainWindow.IsOpen = true;

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

    private void StartReroll()
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

        // Afficher bonus/malus traits si présents
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
        var dice = result.Chosen.ToDisplayString();
        var actionPart = result.Action != null ? $" [{result.Action.Name}]" : "";
        var res = result.Successes == 0 ? "Échec total"
                       : result.Successes == 1 ? "1 succès"
                       : $"{result.Successes} succès";
        var mod = Engine.Modifier != 0
                       ? $" modif {(Engine.Modifier > 0 ? "+" : "")}{Engine.Modifier}"
                       : "";

        return $"[STS] {rank.Label}{actionPart}{mod} · {dice} · palier {result.Palier}+ → {res}";
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

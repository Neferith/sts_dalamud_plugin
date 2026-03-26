using System.Linq;
using System.Text;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using STSPlugin.Windows;

namespace STSPlugin;

public sealed class Plugin : IDalamudPlugin
{
    private const string CmdMain = "/sts";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public Configuration Configuration { get; init; }
    public StsEngine Engine { get; init; }

    private readonly WindowSystem windowSystem = new("STSPlugin");
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Engine = new StsEngine();
        Engine.ChangeRank(Configuration.LastRank);

        mainWindow = new MainWindow(this);
        configWindow = new ConfigWindow(this);

        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(configWindow);

        CommandManager.AddHandler(CmdMain, new CommandInfo(OnCommand)
        {
            HelpMessage = "Ouvre/ferme l'interface STS. \"/sts roll\" lance les dés."
        });

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        CommandManager.RemoveHandler(CmdMain);
        windowSystem.RemoveAllWindows();
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "roll":
            case "r":
                Engine.Roll();
                // Affichage local coloré (visible que par toi)
                PrintStyledLocal();
                // Broadcast dans le canal (visible par tout le monde)
                if (Configuration.EchoToChat)
                    SendToChannel(Configuration.ChatChannel, BuildPlainMessage());
                mainWindow.IsOpen = true;
                break;

            case "config":
                configWindow.Toggle();
                break;

            default:
                ToggleMainUi();
                break;
        }
    }

    /// <summary>
    /// Affichage local uniquement, avec couleurs SeString.
    /// </summary>
    private void PrintStyledLocal()
    {
        var palier = Engine.EffectivePalier;
        var n = Engine.Successes;
        var rank = Engine.Rank;
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
        sb.AddText($"[{rank.Label} · palier {palier}+]");
        sb.AddUiForegroundOff();
        sb.AddText("  ");

        for (var i = 0; i < Engine.CurrentDice.Length; i++)
        {
            var val = Engine.CurrentDice[i];
            var suc = val >= palier;
            if (i > 0) sb.AddText(" · ");
            sb.AddUiForeground(suc ? ColGreen : ColGrey);
            sb.AddText(StsEngine.DispDie(val));
            sb.AddUiForegroundOff();
        }

        sb.AddText("  →  ");

        ushort resCol = n == 0 ? ColRed : n >= 2 ? ColGreen : ColWhite;
        sb.AddUiForeground(resCol);
        sb.AddText(n == 0 ? "Échec total" : n == 1 ? "1 succès" : $"{n} succès");
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

    /// <summary>
    /// Construit le message texte brut pour le canal (le serveur n'accepte pas les SeString).
    /// </summary>
    private string BuildPlainMessage()
    {
        var palier = Engine.EffectivePalier;
        var n = Engine.Successes;
        var rank = Engine.Rank;
        var dice = string.Join(" · ", Engine.CurrentDice.Select(StsEngine.DispDie));
        var res = n == 0 ? "Échec total" : n == 1 ? "1 succès" : $"{n} succès";
        var mod = Engine.Modifier != 0 ? $" modif {(Engine.Modifier > 0 ? "+" : "")}{Engine.Modifier}" : "";

        return $"[STS] {rank.Label}{mod} · {dice} · palier {palier}+ → {res}";
    }

    /// <summary>
    /// Envoie un message texte brut dans un canal via la chatbox native.
    /// </summary>
    private static unsafe void SendToChannel(string channel, string message)
    {
        var uiModule = UIModule.Instance();
        if (uiModule == null)
        {
            Log.Warning("[STS] UIModule introuvable.");
            return;
        }

        var fullMessage = $"/{channel} {message}";
        var bytes = Encoding.UTF8.GetBytes(fullMessage);
        var utf8String = new Utf8String();

        fixed (byte* ptr = bytes)
            utf8String.SetString(ptr);

        uiModule->ProcessChatBoxEntry(&utf8String);
        utf8String.Dtor();
    }

    private void DrawUi() => windowSystem.Draw();
    public void ToggleMainUi() => mainWindow.Toggle();
    public void ToggleConfigUi() => configWindow.Toggle();

    public void SaveRank(string rank)
    {
        Configuration.LastRank = rank;
        Configuration.Save();
    }
}

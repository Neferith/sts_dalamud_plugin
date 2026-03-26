using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
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
                if (Configuration.EchoToChat)
                    PrintDiceResult();
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
    /// Affiche le résultat dans le chat avec le même style que les jets de dés natifs du jeu.
    /// </summary>
    private void PrintDiceResult()
    {
        var palier = Engine.EffectivePalier;
        var n = Engine.Successes;
        var rank = Engine.Rank;
        var player = ClientState.LocalPlayer?.Name.ToString() ?? "???";

        // Couleurs UI FFXIV (IDs de la palette interne du jeu)
        const ushort ColGreen = 43;   // succès
        const ushort ColGrey = 4;    // échec
        const ushort ColRed = 17;   // échec total
        const ushort ColGold = 559;  // label rang / palier
        const ushort ColWhite = 1;    // texte neutre

        var sb = new SeStringBuilder();

        // Nom du joueur (comme le jeu le fait pour /random)
        sb.AddUiForeground(ColWhite);
        sb.AddText(player);
        sb.AddUiForegroundOff();
        sb.AddText(" ");

        // Rang + palier
        sb.AddUiForeground(ColGold);
        sb.AddText($"[{rank.Label} · palier {palier}+]");
        sb.AddUiForegroundOff();
        sb.AddText("  ");

        // Dés un par un, colorés selon succès/échec
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

        // Résultat final
        ushort resCol = n == 0 ? ColRed : n >= 2 ? ColGreen : ColWhite;
        sb.AddUiForeground(resCol);
        sb.AddText(n == 0 ? "Échec total" : n == 1 ? "1 succès" : $"{n} succès");
        sb.AddUiForegroundOff();

        // Rerolls restants si utile
        var rrLeft = Engine.RerollsLeft;
        if (rrLeft > 0)
        {
            sb.AddUiForeground(ColGrey);
            sb.AddText($"  ({rrLeft} reroll{(rrLeft > 1 ? "s" : "")} restant{(rrLeft > 1 ? "s" : "")})");
            sb.AddUiForegroundOff();
        }

        // XivChatType.Dice = type natif des jets de dés du jeu
        ChatGui.Print(new XivChatEntry
        {
            Type = XivChatType.SystemMessage,// (XivChatType)2122,
            Name = SeString.Empty,
            Message = sb.Build(),
        });
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

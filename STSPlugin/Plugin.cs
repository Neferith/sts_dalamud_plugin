using System.Text;
using Dalamud.Game.Command;
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
                    SendToChannel(Configuration.ChatChannel, Engine.ChatSummary());
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
    /// Envoie un message dans un canal de chat via la fonction native du jeu.
    /// Fonctionne comme si le joueur tapait "/{channel} {message}" dans la chatbox.
    /// </summary>
    private static unsafe void SendToChannel(string channel, string message)
    {
        var fullMessage = $"/{channel} {message}";

        var uiModule = UIModule.Instance();
        if (uiModule == null)
        {
            Log.Warning("[STS] UIModule introuvable, impossible d'envoyer dans le chat.");
            return;
        }

        // Encoder en UTF-8 et passer à la chatbox native
        var bytes = Encoding.UTF8.GetBytes(fullMessage);
        var utf8String = new Utf8String();
        fixed (byte* ptr = bytes)
        {
            utf8String.SetString(ptr);
        }

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

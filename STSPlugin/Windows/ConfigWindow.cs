using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

using System;
using System.Numerics;

using STSPlugin.ConfigDomain;

namespace STSPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private static readonly (string Label, string Command)[] Channels =
    [
        ("Dire",                    "say"),
        ("Équipe",                  "party"),
        ("Alliance",                "alliance"),
        ("Lien de linkshell 1",     "ls1"),
        ("Lien de linkshell 2",     "ls2"),
        ("Lien de linkshell 3",     "ls3"),
        ("Lien de linkshell 4",     "ls4"),
        ("Lien de linkshell 5",     "ls5"),
        ("Lien de linkshell 6",     "ls6"),
        ("Lien de linkshell 7",     "ls7"),
        ("Lien de linkshell 8",     "ls8"),
        ("Linkshell inter-monde 1", "cwls1"),
        ("Linkshell inter-monde 2", "cwls2"),
        ("Linkshell inter-monde 3", "cwls3"),
        ("Linkshell inter-monde 4", "cwls4"),
    ];

    public ConfigWindow(Plugin plugin)
        : base("STS — Configuration##sts_config",
               ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)
    {
        this.plugin = plugin;
        Size = new Vector2(400, 260);
    }

    public void Dispose() { }

    public override void Draw()
    {
        // ---- Source des dés ----
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.58f, 1f), "SOURCE DES DÉS");
        ImGui.Separator();
        ImGui.Spacing();

        var isInternal = plugin.Configuration.RollSource == RollSource.Internal;
        var isGameRandom = plugin.Configuration.RollSource == RollSource.GameRandom;

        if (ImGui.RadioButton("Interne (RNG du plugin)", isInternal))
        {
            plugin.Configuration.RollSource = RollSource.Internal;
            plugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("(rapide, non vérifiable)");

        if (ImGui.RadioButton("/random du jeu (0–999)", isGameRandom))
        {
            plugin.Configuration.RollSource = RollSource.GameRandom;
            plugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("(visible par tous, infalsifiable)");

        ImGui.Spacing();
        ImGui.Spacing();

        // ---- Echo dans le chat ----
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.58f, 1f), "CHAT");
        ImGui.Separator();
        ImGui.Spacing();

        var echo = plugin.Configuration.EchoToChat;
        if (ImGui.Checkbox("Poster le résultat dans le chat (/sts roll)", ref echo))
        {
            plugin.Configuration.EchoToChat = echo;
            plugin.Configuration.Save();
        }

        if (!echo) ImGui.BeginDisabled();

        ImGui.TextUnformatted("Canal :");
        ImGui.SameLine();

        var currentCmd = plugin.Configuration.ChatChannel;
        var currentIdx = Array.FindIndex(Channels, c => c.Command == currentCmd);
        if (currentIdx < 0) currentIdx = 0;

        ImGui.SetNextItemWidth(200);
        if (ImGui.BeginCombo("##channel", Channels[currentIdx].Label))
        {
            for (var i = 0; i < Channels.Length; i++)
            {
                var selected = i == currentIdx;
                if (ImGui.Selectable(Channels[i].Label + "##ch" + i, selected))
                {
                    plugin.Configuration.ChatChannel = Channels[i].Command;
                    plugin.Configuration.Save();
                }
                if (selected) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.TextDisabled($"(/{Channels[currentIdx].Command})");

        if (!echo) ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Le résultat est toujours visible dans la fenêtre STS.");
    }
}

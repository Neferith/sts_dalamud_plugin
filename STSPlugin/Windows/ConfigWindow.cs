using Dalamud.Bindings.ImGui;

using System;
using System.Numerics;
using Dalamud.Interface.Windowing;

namespace STSPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    // Channels disponibles : label affiché → commande slash sans le /
    private static readonly (string Label, string Command)[] Channels =
    [
        ("Dire",                  "say"),
        ("Équipe",                "party"),
        ("Alliance",              "alliance"),
        ("Lien de linkshell 1",   "ls1"),
        ("Lien de linkshell 2",   "ls2"),
        ("Lien de linkshell 3",   "ls3"),
        ("Lien de linkshell 4",   "ls4"),
        ("Lien de linkshell 5",   "ls5"),
        ("Lien de linkshell 6",   "ls6"),
        ("Lien de linkshell 7",   "ls7"),
        ("Lien de linkshell 8",   "ls8"),
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
        Size = new Vector2(360, 200);
    }

    public void Dispose() { }

    public override void Draw()
    {
        // ---- Echo activé/désactivé ----
        var echo = plugin.Configuration.EchoToChat;
        if (ImGui.Checkbox("Poster le résultat dans le chat (/sts roll)", ref echo))
        {
            plugin.Configuration.EchoToChat = echo;
            plugin.Configuration.Save();
        }

        // ---- Choix du channel (grisé si echo désactivé) ----
        ImGui.Spacing();

        if (!echo) ImGui.BeginDisabled();

        ImGui.TextUnformatted("Channel :");
        ImGui.SameLine();

        // Trouver l'index courant
        var currentCmd = plugin.Configuration.ChatChannel;
        var currentIdx = Array.FindIndex(Channels, c => c.Command == currentCmd);
        if (currentIdx < 0) currentIdx = 0;

        ImGui.SetNextItemWidth(220);
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

        // ---- Info ----
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Le résultat est aussi toujours visible dans la fenêtre STS.");
    }
}

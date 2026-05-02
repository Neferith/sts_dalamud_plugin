using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

using System;
using System.Numerics;
using System.Text;

using STSPlugin.ConfigDomain;

namespace STSPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    // Buffer ImGui pour le champ URL (max 256 chars)
    private readonly byte[] _urlBuffer = new byte[256];

    private static readonly (string Label, string Command)[] Channels =
    [
        ("Canal en cours",              ""),
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
        Size = new Vector2(440, 400);

        // Pré-remplir le buffer URL avec la valeur persistée
        SyncUrlBufferFromConfig();
    }

    public void Dispose() { }

    public override void Draw()
    {
        // ================================================================
        // SOURCE DES DONNÉES
        // ================================================================
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.58f, 1f), "SOURCE DES DONNÉES");
        ImGui.Separator();
        ImGui.Spacing();

        var isRemote = plugin.Configuration.DataSourceMode == DataSourceMode.Remote;
        var isLocal = plugin.Configuration.DataSourceMode == DataSourceMode.Local;

        if (ImGui.RadioButton("Distant (API)", isRemote))
        {
            plugin.Configuration.DataSourceMode = DataSourceMode.Remote;
            plugin.Configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Local (data.json embarqué)", isLocal))
        {
            plugin.Configuration.DataSourceMode = DataSourceMode.Local;
            plugin.Configuration.Save();
        }

        ImGui.Spacing();

        // Champ URL — grisé en mode Local
        if (isLocal) ImGui.BeginDisabled();

        ImGui.TextUnformatted("URL :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(280);
        if (ImGui.InputText("##backendUrl", _urlBuffer, ImGuiInputTextFlags.None))
        {
            plugin.Configuration.BackendUrl = ReadUrlBuffer();
            plugin.Configuration.Save();
        }

        ImGui.Spacing();

        // Bouton Rafraîchir
        var refreshLabel = isLocal ? "Rafraîchir les données##refresh" : "Rafraîchir depuis l'API##refresh";
        if (ImGui.Button(refreshLabel))
            plugin.ReloadDataSources();

        if (isLocal) ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.Spacing();

        // ================================================================
        // SOURCE DES DÉS
        // ================================================================
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

        // ================================================================
        // CHAT
        // ================================================================
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

    // ------------------------------------------------------------------ Helpers URL buffer

    /// <summary>Copie la valeur persistée dans le buffer ImGui.</summary>
    private void SyncUrlBufferFromConfig()
    {
        Array.Clear(_urlBuffer, 0, _urlBuffer.Length);
        var bytes = Encoding.UTF8.GetBytes(plugin.Configuration.BackendUrl);
        Buffer.BlockCopy(bytes, 0, _urlBuffer, 0, Math.Min(bytes.Length, _urlBuffer.Length - 1));
    }

    private string ReadUrlBuffer()
    {
        var len = Array.IndexOf(_urlBuffer, (byte)0);
        return Encoding.UTF8.GetString(_urlBuffer, 0, len < 0 ? _urlBuffer.Length : len);
    }
}

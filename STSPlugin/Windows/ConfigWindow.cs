using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using STSPlugin.ConfigDomain;
using STSPlugin.UseCases.Auth;
using System;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace STSPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    // Buffers ImGui
    private readonly byte[] _urlBuffer = new byte[256];
    private readonly byte[] _usernameBuffer = new byte[128];
    private readonly byte[] _passwordBuffer = new byte[128];

    private static readonly Vector4 ColMuted = new(0.6f, 0.6f, 0.58f, 1f);
    private static readonly Vector4 ColSuccess = new(0.06f, 0.43f, 0.34f, 1f);
    private static readonly Vector4 ColDanger = new(0.64f, 0.17f, 0.17f, 1f);
    private static readonly Vector4 ColInfo = new(0.09f, 0.37f, 0.65f, 1f);

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
        Size = new Vector2(440, 520);
        SyncBuffersFromConfig();
    }

    public void Dispose() { }

    public override void Draw()
    {
        // ================================================================
        // SOURCE DES DONNÉES
        // ================================================================
        ImGui.TextColored(ColMuted, "SOURCE DES DONNÉES");
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

        if (isLocal) ImGui.BeginDisabled();

        ImGui.TextUnformatted("URL de base :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(240);
        if (ImGui.InputText("##apiBaseUrl", _urlBuffer, ImGuiInputTextFlags.None))
        {
            plugin.Configuration.ApiBaseUrl = ReadBuffer(_urlBuffer);
            plugin.Configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Données : {plugin.Configuration.DataUrl}");

        ImGui.Spacing();

        var refreshLabel = isLocal ? "Rafraîchir##refresh" : "Rafraîchir depuis l'API##refresh";
        if (ImGui.Button(refreshLabel))
            plugin.ReloadDataSources();

        if (isLocal) ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.Spacing();

        // ================================================================
        // COMPTE JOUEUR
        // ================================================================
        ImGui.TextColored(ColMuted, "COMPTE JOUEUR");
        ImGui.Separator();
        ImGui.Spacing();

        var authState = plugin.AuthState;
        var isConnected = authState.IsAuthenticated;

        // Statut
        if (isConnected)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess);
            ImGui.Text("●");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.Text($"Connecté en tant que {authState.Username}");
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
            ImGui.Text("●");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.TextColored(ColMuted, "Non connecté");
        }

        ImGui.Spacing();

        ImGui.TextUnformatted("Nom d'utilisateur :");
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("##playerUsername", _usernameBuffer, ImGuiInputTextFlags.None))
        {
            plugin.Configuration.PlayerUsername = ReadBuffer(_usernameBuffer);
            plugin.Configuration.Save();
        }

        ImGui.TextUnformatted("Mot de passe :");
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("##playerPassword", _passwordBuffer, ImGuiInputTextFlags.Password))
        {
            plugin.Configuration.PlayerPassword = ReadBuffer(_passwordBuffer);
            plugin.Configuration.Save();
        }

        ImGui.Spacing();

        if (authState.LastError is { } error)
        {
            ImGui.TextColored(ColDanger, error);
            ImGui.Spacing();
        }

        var canConnect = !string.IsNullOrWhiteSpace(plugin.Configuration.PlayerUsername)
                      && !string.IsNullOrWhiteSpace(plugin.Configuration.PlayerPassword)
                      && plugin.Configuration.DataSourceMode == DataSourceMode.Remote;

        if (isConnected)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.40f));
            if (ImGui.Button("Se déconnecter##logout"))
                plugin.Logout.Execute();
            ImGui.PopStyleColor(2);
        }
        else
        {
            if (!canConnect)
            {
                ImGui.BeginDisabled();
                ImGui.TextDisabled(plugin.Configuration.DataSourceMode == DataSourceMode.Local
                    ? "(mode local — connexion désactivée)"
                    : "(remplir les champs ci-dessus)");
                ImGui.Spacing();
            }

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.09f, 0.37f, 0.65f, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColInfo);
            if (ImGui.Button("Se connecter##login") && canConnect)
            {
                var username = plugin.Configuration.PlayerUsername;
                var password = plugin.Configuration.PlayerPassword;
                _ = Task.Run(() => plugin.Login.ExecuteAsync(username, password));
            }
            ImGui.PopStyleColor(3);

            if (!canConnect) ImGui.EndDisabled();
        }

        ImGui.Spacing();
        ImGui.Spacing();

        // ================================================================
        // SOURCE DES DÉS
        // ================================================================
        ImGui.TextColored(ColMuted, "SOURCE DES DÉS");
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
        ImGui.TextColored(ColMuted, "CHAT");
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SyncBuffersFromConfig()
    {
        WriteBuffer(_urlBuffer, plugin.Configuration.ApiBaseUrl);
        WriteBuffer(_usernameBuffer, plugin.Configuration.PlayerUsername);
        WriteBuffer(_passwordBuffer, plugin.Configuration.PlayerPassword);
    }

    private static void WriteBuffer(byte[] buffer, string value)
    {
        Array.Clear(buffer, 0, buffer.Length);
        var bytes = Encoding.UTF8.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, buffer, 0, Math.Min(bytes.Length, buffer.Length - 1));
    }

    private static string ReadBuffer(byte[] buffer)
    {
        var len = Array.IndexOf(buffer, (byte)0);
        return Encoding.UTF8.GetString(buffer, 0, len < 0 ? buffer.Length : len);
    }
}

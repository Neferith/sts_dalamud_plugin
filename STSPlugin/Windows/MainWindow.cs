using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Sts.Domain;
using System;
using System.Linq;
using System.Numerics;

namespace STSPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private StsEngine Engine => plugin.Engine;

    // Couleurs
    private static readonly Vector4 ColSuccess = new(0.06f, 0.43f, 0.34f, 1f);
    private static readonly Vector4 ColSuccessBg = new(0.06f, 0.43f, 0.34f, 0.15f);
    private static readonly Vector4 ColFail = new(0.55f, 0.55f, 0.55f, 1f);
    private static readonly Vector4 ColFailBg = new(0.55f, 0.55f, 0.55f, 0.08f);
    private static readonly Vector4 ColDanger = new(0.64f, 0.17f, 0.17f, 1f);
    private static readonly Vector4 ColMuted = new(0.60f, 0.60f, 0.58f, 1f);
    private static readonly Vector4 ColInfo = new(0.09f, 0.37f, 0.65f, 1f);
    private static readonly Vector4 ColWarn = new(0.52f, 0.31f, 0.04f, 1f);
    private static readonly Vector4 ColActive = new(0.20f, 0.20f, 0.20f, 0.40f);

    // État onglet personnages
    private string _newCharName = string.Empty;
    private Guid? _selectedId = null;

    public MainWindow(Plugin plugin)
        : base("STS — Système Très Simple##sts_main",
               ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(330, 420),
            MaximumSize = new Vector2(520, 860),
        };
        Size = new Vector2(370, 560);
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (!ImGui.BeginTabBar("##sts_main_tabs")) return;

        if (ImGui.BeginTabItem("Dés##tab_dice"))
        {
            ImGui.Spacing();
            DrawDiceTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Personnages##tab_chars"))
        {
            ImGui.Spacing();
            DrawCharacterTab();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    // ================================================================== Onglet Dés

    private void DrawDiceTab()
    {
        var active = plugin.GetActiveCharacter.Execute();

        if (active is null)
        {
            ImGui.TextColored(ColMuted, "Aucun personnage actif.");
            ImGui.TextColored(ColMuted, "Sélectionnez-en un dans l'onglet Personnages.");
            return;
        }

        DrawActiveCharacterHeader(active);
        ImGui.Spacing();
        DrawStats();
        ImGui.Spacing();
        DrawModeRow();
        ImGui.Spacing();
        DrawModifierRow();
        ImGui.Spacing();
        DrawRollButton();
        ImGui.Spacing();
        if (Engine.HasRolled)
        {
            DrawDice();
            ImGui.Spacing();
            DrawRerollButton();
            ImGui.Spacing();
            DrawResult();
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            DrawResetButton();
        }
        DrawHistory();
    }

    // ------------------------------------------------------------------ En-tête personnage actif

    private void DrawActiveCharacterHeader(Character active)
    {
        var rank = Rank.Get(active.RankKey);

        ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess);
        ImGui.Text("●");
        ImGui.PopStyleColor();
        ImGui.SameLine();

        ImGui.Text(active.Name);
        ImGui.SameLine();

        ImGui.TextColored(ColMuted, $"— {rank.Label}");
    }

    // ------------------------------------------------------------------ Stats

    private void DrawStats()
    {
        var rank = Engine.CurrentRank;
        var rrLeft = Engine.RerollsLeft;

        ImGui.BeginGroup();

        ImGui.TextColored(ColMuted, "Palier");
        ImGui.SameLine();
        ImGui.Text($"{Engine.EffectivePalier}+");
        ImGui.SameLine(); ImGui.Spacing(); ImGui.SameLine();

        ImGui.TextColored(ColMuted, "Rerolls");
        ImGui.SameLine();
        ImGui.TextColored(rrLeft > 0 ? ColInfo : ColFail, $"{rrLeft}/{rank.Rerolls}");
        ImGui.SameLine(); ImGui.Spacing(); ImGui.SameLine();

        ImGui.TextColored(ColMuted, "Traits");
        ImGui.SameLine();
        ImGui.Text($"{rank.Traits}");

        ImGui.EndGroup();
    }

    // ------------------------------------------------------------------ Mode

    private void DrawModeRow()
    {
        ImGui.TextColored(ColMuted, "Mode :");
        ImGui.SameLine();
        ModeButton("Normal", RollMode.Normal);
        ImGui.SameLine();
        ModeButton("Avantage", RollMode.Avantage);
        ImGui.SameLine();
        ModeButton("Désavantage", RollMode.Desavantage);
    }

    private void ModeButton(string label, RollMode mode)
    {
        var active = Engine.Mode == mode;
        if (active)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ColActive);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive);
        }
        if (ImGui.Button(label + "##mode"))
            Engine.Mode = mode;
        if (active) ImGui.PopStyleColor(2);
    }

    // ------------------------------------------------------------------ Modificateur

    private void DrawModifierRow()
    {
        ImGui.TextColored(ColMuted, "Modif MJ :");
        ImGui.SameLine();

        if (ImGui.Button("−##mod_m"))
            Engine.Modifier = Math.Max(-3, Engine.Modifier - 1);
        ImGui.SameLine();

        var mod = Engine.Modifier;
        var modStr = mod == 0 ? "0" : mod > 0 ? $"+{mod}" : $"{mod}";
        var modCol = mod > 0 ? ColSuccess : mod < 0 ? ColDanger : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        ImGui.TextColored(modCol, modStr);
        ImGui.SameLine();

        if (ImGui.Button("+##mod_p"))
            Engine.Modifier = Math.Min(3, Engine.Modifier + 1);

        if (mod != 0)
        {
            ImGui.SameLine();
            var help = mod > 0 ? $"Palier facilité → {Engine.EffectivePalier}+" : $"Palier durci → {Engine.EffectivePalier}+";
            ImGui.TextColored(ColMuted, help);
        }
    }

    // ------------------------------------------------------------------ Bouton lancer

    private void DrawRollButton()
    {
        var avail = ImGui.GetContentRegionAvail().X;
        if (ImGui.Button("Lancer les dés##roll", new Vector2(avail, 0)))
            plugin.StartRoll(null);
    }

    // ------------------------------------------------------------------ Dés

    private void DrawDice()
    {
        if (Engine.LastResult is not { } result) return;

        DrawDiceSet(result.Chosen, result.Palier, chosen: true);

        if (result.Rejected is { } rejected)
        {
            ImGui.SameLine();
            ImGui.TextColored(ColMuted, " vs");
            ImGui.SameLine();
            DrawDiceSet(rejected, result.Palier, chosen: false);
        }
    }

    private void DrawDiceSet(DiceSet diceSet, int palier, bool chosen)
    {
        var alpha = chosen ? 1f : 0.3f;

        ImGui.BeginGroup();
        foreach (var val in diceSet.Values)
        {
            var suc = val >= palier;
            var label = DiceSet.Display(val);
            var col = suc ? ColSuccess : ColFail;
            var bgCol = suc ? ColSuccessBg : ColFailBg;

            ImGui.PushStyleColor(ImGuiCol.Text, col with { W = alpha });
            ImGui.PushStyleColor(ImGuiCol.Button, bgCol with { W = alpha * 0.6f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, bgCol with { W = alpha * 0.6f });
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, bgCol with { W = alpha * 0.6f });

            ImGui.Button(label + "##die_" + val + "_" + chosen, new Vector2(52, 52));

            ImGui.PopStyleColor(4);
            ImGui.SameLine();
        }
        ImGui.EndGroup();
    }

    // ------------------------------------------------------------------ Reroll

    private void DrawRerollButton()
    {
        if (!Engine.HasRolled) return;

        var left = Engine.RerollsLeft;
        var avail = ImGui.GetContentRegionAvail().X;

        if (left <= 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.3f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.3f));
            ImGui.Button("↺ Reroll (0 restant)##reroll", new Vector2(avail, 0));
            ImGui.PopStyleColor(2);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.09f, 0.37f, 0.65f, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColInfo);
            var s = left == 1 ? "restant" : "restants";
            if (ImGui.Button($"↺ Reroll — relancer les 3 dés  ({left} {s})##reroll", new Vector2(avail, 0)))
                plugin.StartReroll();
            ImGui.PopStyleColor(3);
        }
    }

    // ------------------------------------------------------------------ Résultat

    private void DrawResult()
    {
        if (Engine.LastResult is not { } result) return;

        var col = result.Successes == 0 ? ColDanger
                : result.Successes >= 2 ? ColSuccess
                : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

        if (Engine.Mode != RollMode.Normal)
        {
            var tag = Engine.Mode == RollMode.Avantage ? "Avantage" : "Désavantage";
            ImGui.TextColored(ColMuted, tag + " — meilleur set retenu");
        }

        ImGui.TextColored(col, result.Successes.ToString());
        ImGui.SameLine();

        var lbl = result.Successes == 0 ? "Aucune réussite"
                : result.Successes == 1 ? "réussite"
                : "réussites";
        ImGui.TextColored(ColMuted, $"{lbl}  ·  palier {result.Palier}+");
    }

    // ------------------------------------------------------------------ Reset event

    private void DrawResetButton()
    {
        var avail = ImGui.GetContentRegionAvail().X;
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.52f, 0.31f, 0.04f, 0.20f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.52f, 0.31f, 0.04f, 0.35f));
        ImGui.PushStyleColor(ImGuiCol.Text, ColWarn);
        if (ImGui.Button("Nouvel event — réinitialiser les rerolls##reset", new Vector2(avail, 0)))
            Engine.ResetEvent();
        ImGui.PopStyleColor(3);
    }

    // ------------------------------------------------------------------ Historique

    private void DrawHistory()
    {
        if (Engine.History.Count == 0) return;

        ImGui.Spacing();
        ImGui.TextColored(ColMuted, "HISTORIQUE");
        ImGui.Separator();

        foreach (var entry in Engine.History)
        {
            var col = entry.TotalSuccesses == 0 ? ColDanger
                    : entry.TotalSuccesses >= 2 ? ColSuccess
                    : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

            ImGui.TextColored(ColMuted, entry.RankLabel);
            ImGui.SameLine();
            ImGui.Text(entry.Dice.ToDisplayString());
            ImGui.SameLine();
            ImGui.TextColored(ColMuted, $"({entry.Palier}+)");
            ImGui.SameLine();
            if (entry.ActionName != null)
            {
                ImGui.TextColored(ColMuted, entry.ActionName);
                ImGui.SameLine();
            }
            ImGui.TextColored(col, $"{entry.TotalSuccesses} ✓");
        }
    }

    // ================================================================== Onglet Personnages

    private void DrawCharacterTab()
    {
        var characters = plugin.GetAllCharacters.Execute();
        var activeId = plugin.Configuration.ActiveCharacterId;

        // ---- Liste des personnages ----
        ImGui.TextColored(ColMuted, "PERSONNAGES");
        ImGui.Separator();
        ImGui.Spacing();

        if (characters.Count == 0)
        {
            ImGui.TextColored(ColMuted, "Aucun personnage. Créez-en un ci-dessous.");
            ImGui.Spacing();
        }
        else
        {
            foreach (var character in characters)
            {
                var isActive = character.Id == activeId;
                var isSelected = character.Id == _selectedId;

                // Indicateur actif
                if (isActive)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess);
                    ImGui.Text("●");
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.TextColored(ColMuted, "○");
                }
                ImGui.SameLine();

                // Sélection
                if (ImGui.Selectable(
                    $"{character.Name}  [{Rank.Get(character.RankKey).Label}]##{character.Id}",
                    isSelected))
                {
                    _selectedId = isSelected ? null : character.Id;
                }

                // Double-clic → activer
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    plugin.SetActiveCharacter.Execute(character.Id);
                    plugin.RefreshEquippedTraits();
                    _selectedId = character.Id;
                }
            }
        }

        ImGui.Spacing();

        // ---- Actions sur la sélection ----
        if (_selectedId is { } selectedId && characters.Any(c => c.Id == selectedId))
        {
            var selected = characters.First(c => c.Id == selectedId);
            var isActive = selected.Id == activeId;

            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextColored(ColMuted, $"Sélectionné : {selected.Name}");
            ImGui.Spacing();

            // Activer / Désactiver
            var avail = ImGui.GetContentRegionAvail().X;
            if (!isActive)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.09f, 0.37f, 0.65f, 0.25f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, ColInfo);
                if (ImGui.Button("Activer ce personnage##activate", new Vector2(avail, 0)))
                    plugin.SetActiveCharacter.Execute(selectedId);
                ImGui.PopStyleColor(3);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.52f, 0.31f, 0.04f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.52f, 0.31f, 0.04f, 0.35f));
                ImGui.PushStyleColor(ImGuiCol.Text, ColWarn);
                if (ImGui.Button("Désactiver##deactivate", new Vector2(avail, 0)))
                    plugin.SetActiveCharacter.Execute(null);
                ImGui.PopStyleColor(3);
            }

            ImGui.Spacing();

            // Ouvrir la fiche
          //  var avail = ImGui.GetContentRegionAvail().X;
            if (ImGui.Button("Ouvrir la fiche##open_char", new Vector2(avail, 0)))
                plugin.OpenCharacterWindow(selected);

            ImGui.Spacing();


            // Supprimer
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
            if (ImGui.Button($"Supprimer {selected.Name}##delete", new Vector2(avail, 0)))
            {
                plugin.DeleteCharacter.Execute(selectedId);
                _selectedId = null;
            }
            ImGui.PopStyleColor(3);

            ImGui.Spacing();
        }

        // ---- Créer un nouveau personnage ----
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(ColMuted, "NOUVEAU PERSONNAGE");
        ImGui.Spacing();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 90);
        ImGui.InputText("##newname", ref _newCharName, 64);
        ImGui.SameLine();

        var canCreate = !string.IsNullOrWhiteSpace(_newCharName);
        if (!canCreate)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.3f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.3f));
        }
        if (ImGui.Button("Créer##create") && canCreate)
        {
            var created = plugin.CreateCharacter.Execute(_newCharName, RankKey.Novice);
            _selectedId = created.Id;
            _newCharName = string.Empty;
        }
        if (!canCreate) ImGui.PopStyleColor(2);
    }
}

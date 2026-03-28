using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using STSPlugin.Domain;
using System;
using System.Linq;
using System.Numerics;

namespace STSPlugin.Windows;

/// <summary>
/// Fenêtre de fiche personnage. Une instance par personnage ouvert.
/// </summary>
public class CharacterWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private readonly Character _character;

    // --- état UI ---
    private bool _editMode = false;
    private string _editName = string.Empty;

    // Couleurs
    private static readonly Vector4 ColSuccess = new(0.06f, 0.43f, 0.34f, 1f);
    private static readonly Vector4 ColDanger = new(0.64f, 0.17f, 0.17f, 1f);
    private static readonly Vector4 ColMuted = new(0.60f, 0.60f, 0.58f, 1f);
    private static readonly Vector4 ColInfo = new(0.09f, 0.37f, 0.65f, 1f);
    private static readonly Vector4 ColWarn = new(0.52f, 0.31f, 0.04f, 1f);
    private static readonly Vector4 ColActive = new(0.20f, 0.20f, 0.20f, 0.40f);

    public CharacterWindow(Plugin plugin, Character character)
        : base($"{character.Name} — Fiche STS##{character.Id}")
    {
        _plugin = plugin;
        _character = character;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(900, 1200),
        };
        Size = new Vector2(500, 650);
    }

    public void Dispose() { }

    public override void Draw()
    {
        DrawHeader();
        ImGui.Separator();
        ImGui.Spacing();

        if (_editMode)
            DrawEditMode();
        else
            DrawReadMode();
    }

    // ------------------------------------------------------------------ En-tête

    private void DrawHeader()
    {
        var isActive = _plugin.Configuration.ActiveCharacterId == _character.Id;

        // Indicateur actif
        if (isActive)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess);
            ImGui.Text("●");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.TextColored(ColMuted, "Personnage actif");
        }
        else
        {
            ImGui.TextColored(ColMuted, "○ Inactif");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.09f, 0.37f, 0.65f, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColInfo);
            if (ImGui.Button("Activer##hdr_activate"))
                _plugin.SetActiveCharacter.Execute(_character.Id);
            ImGui.PopStyleColor(3);
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - 80);

        if (_editMode)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.06f, 0.43f, 0.34f, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.06f, 0.43f, 0.34f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess);
            if (ImGui.Button("✓ Sauver##save"))
            {
                if (!string.IsNullOrWhiteSpace(_editName))
                    _character.Name = _editName.Trim();
                _plugin.UpdateCharacter.Execute(_character);
                // Mettre à jour le titre de la fenêtre
                WindowName = $"{_character.Name} — Fiche STS##{_character.Id}";
                _editMode = false;
            }
            ImGui.PopStyleColor(3);
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.40f));
            if (ImGui.Button("✕ Annuler##cancel"))
                _editMode = false;
            ImGui.PopStyleColor(2);
        }
        else
        {
            if (ImGui.Button("✎ Éditer##edit"))
            {
                _editName = _character.Name;
                _editMode = true;
            }
        }
    }

    // ------------------------------------------------------------------ Mode lecture

    private void DrawReadMode()
    {
        var rank = Rank.Get(_character.RankKey);

        // Nom + rang
        ImGui.Text(_character.Name);
        ImGui.SameLine();
        ImGui.TextColored(ColMuted, $"— {rank.Label}  ·  palier {rank.Palier}+  ·  {rank.Rerolls} reroll(s)  ·  {rank.Traits} traits");

        ImGui.Spacing();

        // Job
        var jobLabel = _character.Job == Job.Aucun ? "Aucun job" : _character.Job.ToString();
        ImGui.TextColored(ColMuted, "Job :");
        ImGui.SameLine();
        ImGui.Text(jobLabel);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Trait d'origine
        ImGui.TextColored(ColMuted, "TRAIT D'ORIGINE");
        ImGui.Spacing();
        if (_character.OriginTrait is { } originId)
        {
            var origin = Trait.Get(originId);
            ImGui.Text($"● {origin.Name}");
            ImGui.PushStyleColor(ImGuiCol.Text, ColMuted);
            ImGui.TextWrapped(origin.Description);
            ImGui.PopStyleColor();
        }
        else
        {
            ImGui.TextColored(ColMuted, "Aucun trait d'origine.");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Traits équipés
        ImGui.TextColored(ColMuted, $"TRAITS  ({_character.EquippedTraits.Count}/{rank.Traits})");
        ImGui.Spacing();
        if (_character.EquippedTraits.Count == 0)
        {
            ImGui.TextColored(ColMuted, "Aucun trait équipé.");
        }
        else
        {
            foreach (var traitId in _character.EquippedTraits)
            {
                var trait = Trait.Get(traitId);
                ImGui.Text($"● {trait.Name}");
                ImGui.PushStyleColor(ImGuiCol.Text, ColMuted);
                ImGui.TextWrapped(trait.Description);
                ImGui.PopStyleColor();
                ImGui.Spacing();
            }
        }
    }

    // ------------------------------------------------------------------ Mode édition

    private void DrawEditMode()
    {
        var rank = Rank.Get(_character.RankKey);

        // ---- Nom ----
        ImGui.TextColored(ColMuted, "Nom :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        ImGui.InputText("##edit_name", ref _editName, 64);

        ImGui.Spacing();

        // ---- Rang ----
        ImGui.TextColored(ColMuted, "Rang :");
        ImGui.Spacing();
        foreach (var rankKey in Enum.GetValues<RankKey>())
        {
            var current = _character.RankKey == rankKey;
            if (current) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button(Rank.Get(rankKey).Label + "##rk_" + rankKey))
            {
                _character.RankKey = rankKey;
                if (_plugin.Configuration.ActiveCharacterId == _character.Id)
                    _plugin.Engine.ChangeRank(rankKey);
            }
            if (current) ImGui.PopStyleColor(2);
            ImGui.SameLine();
        }
        ImGui.NewLine();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ---- Job ----
        ImGui.TextColored(ColMuted, "Job :");
        ImGui.Spacing();
        foreach (var job in Enum.GetValues<Job>())
        {
            var current = _character.Job == job;
            if (current) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button(job.ToString() + "##job_" + job))
                _plugin.SetJob.Execute(_character, job);
            if (current) ImGui.PopStyleColor(2);
            ImGui.SameLine();
        }
        ImGui.NewLine();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ---- Trait d'origine ----
        ImGui.TextColored(ColMuted, "TRAIT D'ORIGINE");
        ImGui.TextColored(ColMuted, "(gratuit, hors quota, nécessite la certification MJ)");
        ImGui.Spacing();

        if (_character.OriginTrait is { } currentOrigin)
        {
            ImGui.Text($"● {Trait.Get(currentOrigin).Name}");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
            if (ImGui.Button("Retirer##origin_remove"))
                _plugin.SetOriginTrait.Execute(_character, null);
            ImGui.PopStyleColor(3);
        }
        else
        {
            ImGui.TextColored(ColMuted, "Aucun. Choisissez ci-dessous :");
            ImGui.Spacing();
            foreach (var trait in Trait.GetByCategory(TraitCategory.Origine))
            {
                if (ImGui.Button($"+ {trait.Name}##orig_{trait.Id}"))
                    _plugin.SetOriginTrait.Execute(_character, trait.Id);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(trait.Description);
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ---- Traits équipés ----
        ImGui.TextColored(ColMuted, $"TRAITS ÉQUIPÉS  ({_character.EquippedTraits.Count}/{rank.Traits})");
        ImGui.Spacing();

        if (_character.EquippedTraits.Count == 0)
        {
            ImGui.TextColored(ColMuted, "Aucun trait équipé.");
        }
        else
        {
            foreach (var traitId in _character.EquippedTraits.ToList())
            {
                var trait = Trait.Get(traitId);
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                if (ImGui.Button($"✕##remove_{traitId}"))
                    _plugin.UnequipTrait.Execute(_character, traitId);
                ImGui.PopStyleColor(3);
                ImGui.SameLine();
                ImGui.Text(trait.Name);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(trait.Description);
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ---- Traits disponibles ----
        if (_character.FreeTraitSlots > 0)
        {
            ImGui.TextColored(ColMuted, $"TRAITS DISPONIBLES  ({_character.FreeTraitSlots} slot(s) libre(s))");
            ImGui.Spacing();

            var categories = new[]
            {
                TraitCategory.Connaissance,
                TraitCategory.RoleDps,
                TraitCategory.RoleSoigneur,
                TraitCategory.RoleTank,
                TraitCategory.Job,
            };

            foreach (var category in categories)
            {
                var available = Trait.GetByCategory(category)
                    .Where(t => !_character.HasTrait(t.Id))
                    .Where(t => t.RequiredJob == null || t.RequiredJob == _character.Job)
                    .ToList();

                if (available.Count == 0) continue;

                ImGui.TextColored(ColMuted, CategoryLabel(category));
                ImGui.Spacing();

                foreach (var trait in available)
                {
                    var canEquip = _character.CanEquip(trait.Id);
                    if (!canEquip)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                        ImGui.PushStyleColor(ImGuiCol.Text, ColMuted);
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.09f, 0.37f, 0.65f, 0.20f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
                        ImGui.PushStyleColor(ImGuiCol.Text, ColInfo);
                    }

                    if (ImGui.Button($"+ {trait.Name}##avail_{trait.Id}") && canEquip)
                        _plugin.EquipTrait.Execute(_character, trait.Id);

                    ImGui.PopStyleColor(3);

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(trait.Description);
                }
                ImGui.Spacing();
            }
        }
        else
        {
            ImGui.TextColored(ColMuted, "Tous les slots de traits sont utilisés.");
        }
    }

    // ------------------------------------------------------------------ Helpers

    private static string CategoryLabel(TraitCategory category) => category switch
    {
        TraitCategory.Connaissance => "Connaissances",
        TraitCategory.RoleDps => "Rôle — DPS",
        TraitCategory.RoleSoigneur => "Rôle — Soigneur",
        TraitCategory.RoleTank => "Rôle — Tank",
        TraitCategory.Job => "Job",
        _ => category.ToString(),
    };
}

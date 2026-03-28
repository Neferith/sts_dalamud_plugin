using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using STSPlugin.Domain;
using STSPlugin.UseCases;
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
        var job = _character.JobId != null ? _plugin.JobRepository.GetById(_character.JobId) : null;

        // Nom + rang
        ImGui.Text(_character.Name);
        ImGui.SameLine();
        ImGui.TextColored(ColMuted, $"— {rank.Label}  ·  palier {rank.Palier}+  ·  {rank.Rerolls} reroll(s)  ·  {rank.Traits} traits");

        ImGui.Spacing();

        // Job
        ImGui.TextColored(ColMuted, "Job :");
        ImGui.SameLine();
        ImGui.Text(job?.Name ?? "Aucun");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Trait d'origine
        ImGui.TextColored(ColMuted, "TRAIT D'ORIGINE");
        ImGui.Spacing();
        if (_character.OriginTraitId is { } originId)
        {
            var origin = _plugin.TraitRepository.GetById(originId);
            if (origin != null)
            {
                ImGui.Text($"● {origin.Name}");
                ImGui.PushStyleColor(ImGuiCol.Text, ColMuted);
                ImGui.TextWrapped(origin.Description);
                ImGui.PopStyleColor();
            }
        }
        else
        {
            ImGui.TextColored(ColMuted, "Aucun trait d'origine.");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Traits équipés
        ImGui.TextColored(ColMuted, $"TRAITS  ({_character.EquippedTraitIds.Count}/{rank.Traits})");
        ImGui.Spacing();
        if (_character.EquippedTraitIds.Count == 0)
        {
            ImGui.TextColored(ColMuted, "Aucun trait équipé.");
        }
        else
        {
            foreach (var traitId in _character.EquippedTraitIds)
            {
                var trait = _plugin.TraitRepository.GetById(traitId);
                if (trait is null) continue;
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

        // Bouton "Aucun"
        var noJob = _character.JobId == null;
        if (noJob) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
        if (ImGui.Button("Aucun##job_none"))
            _plugin.SetJob.Execute(_character, null);
        if (noJob) ImGui.PopStyleColor(2);
        ImGui.SameLine();

        foreach (var job in _plugin.JobRepository.GetAll())
        {
            var current = _character.JobId == job.Id;
            if (current) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button(job.Name + "##job_" + job.Id))
                _plugin.SetJob.Execute(_character, job.Id);
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

        if (_character.OriginTraitId is { } currentOriginId)
        {
            var origin = _plugin.TraitRepository.GetById(currentOriginId);
            ImGui.Text($"● {origin?.Name ?? currentOriginId}");
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
            foreach (var trait in _plugin.TraitRepository.GetByCategory(TraitCategory.Origine))
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
        ImGui.TextColored(ColMuted, $"TRAITS ÉQUIPÉS  ({_character.EquippedTraitIds.Count}/{rank.Traits})");
        ImGui.Spacing();

        if (_character.EquippedTraitIds.Count == 0)
        {
            ImGui.TextColored(ColMuted, "Aucun trait équipé.");
        }
        else
        {
            foreach (var traitId in _character.EquippedTraitIds.ToList())
            {
                var trait = _plugin.TraitRepository.GetById(traitId);
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                if (ImGui.Button($"✕##remove_{traitId}"))
                    _plugin.UnequipTrait.Execute(_character, traitId);
                ImGui.PopStyleColor(3);
                ImGui.SameLine();
                ImGui.Text(trait?.Name ?? traitId);
                if (ImGui.IsItemHovered() && trait != null)
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
                var available = _plugin.TraitRepository.GetByCategory(category)
                    .Where(t => !_character.HasTrait(t.Id))
                    .Where(t => t.RequiredJobId == null || t.RequiredJobId == _character.JobId)
                    .ToList();

                if (available.Count == 0) continue;

                ImGui.TextColored(ColMuted, CategoryLabel(category));
                ImGui.Spacing();

                foreach (var trait in available)
                {
                    // Vérifier l'exclusivité localement pour griser le bouton
                    var hasConflict = trait.ExclusiveGroup != null &&
                        _character.EquippedTraitIds
                            .Select(id => _plugin.TraitRepository.GetById(id))
                            .Any(t => t?.ExclusiveGroup == trait.ExclusiveGroup);

                    var canEquip = !hasConflict;

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

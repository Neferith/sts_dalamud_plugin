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
    private int _editSkillPoints = 0;

    // --- état UI certifications ---
    private string _newCertName = string.Empty;
    private string _newCertOriginTraitId = string.Empty;
    private string _newCertAbilityId = string.Empty;
    private int _newCertFreePoints = 0;

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
        SizeCondition = ImGuiCond.FirstUseEver;
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
            {
                _plugin.SetActiveCharacter.Execute(_character.Id);
                _plugin.RefreshEquippedTraits();
            }
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
                _plugin.SetSkillPoints.Execute(_character, _editSkillPoints);
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
                _editSkillPoints = _character.SkillPoints;
                _editMode = true;
            }
        }
    }

    // ================================================================== Mode lecture

    private void DrawReadMode()
    {
        var rank = Rank.Get(_character.RankKey);
        var job = _character.JobId != null ? _plugin.JobRepository.GetById(_character.JobId) : null;

        ImGui.Text(_character.Name);
        ImGui.SameLine();
        ImGui.TextColored(ColMuted, $"— {rank.Label}  ·  palier {rank.Palier}+  ·  {rank.Rerolls} reroll(s)  ·  {rank.Traits} traits");

        ImGui.Spacing();
        ImGui.TextColored(ColMuted, "Job :");
        ImGui.SameLine();
        ImGui.Text(job?.Name ?? "Aucun");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawReadCertifications();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Trait d'origine
        ImGui.TextColored(ColMuted, "TRAIT D'ORIGINE");
        ImGui.Spacing();
        if (_character.OriginTraitId is { } originId)
        {
            var origin = _plugin.TraitRepository.GetById(originId);
            var hasCert = _character.HasCertificationForOriginTrait(originId);
            if (origin != null)
            {
                ImGui.Text($"● {origin.Name}");
                if (hasCert) { ImGui.SameLine(); ImGui.TextColored(ColSuccess, "(certifié — gratuit)"); }
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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawReadAbilities();
    }

    private void DrawReadCertifications()
    {
        ImGui.TextColored(ColMuted, $"CERTIFICATIONS  ({_character.Certifications.Count})");
        ImGui.Spacing();

        if (_character.Certifications.Count == 0)
        {
            ImGui.TextColored(ColMuted, "Aucune certification.");
            return;
        }

        foreach (var cert in _character.Certifications)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess);
            ImGui.Text("★");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.Text(cert.Name);

            if (cert.LinkedOriginTraitId != null)
            {
                var trait = _plugin.TraitRepository.GetById(cert.LinkedOriginTraitId);
                ImGui.SameLine();
                ImGui.TextColored(ColMuted, $"→ Trait d'origine : {trait?.Name ?? cert.LinkedOriginTraitId}");
            }
            if (cert.LinkedAbilityId != null && cert.FreePoints > 0)
            {
                var ability = _plugin.AbilityRepository.GetById(cert.LinkedAbilityId);
                ImGui.SameLine();
                ImGui.TextColored(ColMuted, $"→ {cert.FreePoints} pt(s) gratuit(s) : {ability?.Name ?? cert.LinkedAbilityId}");
            }
        }
    }

    private void DrawReadAbilities()
    {
        ImGui.TextColored(ColMuted, $"COMPÉTENCES  (points : {_character.SpentSkillPoints} / {_character.SkillPoints})");
        ImGui.Spacing();

        if (_character.EquippedAbilities.Count == 0)
        {
            ImGui.TextColored(ColMuted, "Aucune compétence apprise.");
            return;
        }

        foreach (var equipped in _character.EquippedAbilities)
        {
            var ability = _plugin.AbilityRepository.GetById(equipped.AbilityId);
            if (ability is null) continue;
            var freePoints = _character.GetFreePointsForAbility(equipped.AbilityId);
            var levelData = ability.Levels.FirstOrDefault(l => l.Level == equipped.Level);

            ImGui.Text($"● {ability.Name}");
            ImGui.SameLine();
            ImGui.TextColored(ColInfo, $"Lv{equipped.Level}");

            if (freePoints > 0)
            {
                ImGui.SameLine();
                ImGui.TextColored(ColSuccess, $"({freePoints} pt(s) certif. gratuit(s))");
            }
            if (ability.UsageLimit != UsageLimit.None)
            {
                ImGui.SameLine();
                ImGui.TextColored(ColWarn, UsageLimitLabel(ability.UsageLimit));
            }
            if (levelData != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ColMuted);
                ImGui.TextWrapped(levelData.Description);
                ImGui.PopStyleColor();
            }
            ImGui.Spacing();
        }
    }

    // ================================================================== Mode édition

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
        var noJob = _character.JobId == null;
        if (noJob) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
        if (ImGui.Button("Aucun##job_none")) _plugin.SetJob.Execute(_character, null);
        if (noJob) ImGui.PopStyleColor(2);
        ImGui.SameLine();
        foreach (var job in _plugin.JobRepository.GetAll())
        {
            var current = _character.JobId == job.Id;
            if (current) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button(job.Name + "##job_" + job.Id)) _plugin.SetJob.Execute(_character, job.Id);
            if (current) ImGui.PopStyleColor(2);
            ImGui.SameLine();
        }
        ImGui.NewLine();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ---- Certifications ----
        DrawEditCertifications();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ---- Trait d'origine ----
        ImGui.TextColored(ColMuted, "TRAIT D'ORIGINE");
        ImGui.TextColored(ColMuted, "(gratuit si certifié, sinon nécessite la certification MJ)");
        ImGui.Spacing();

        if (_character.OriginTraitId is { } currentOriginId)
        {
            var origin = _plugin.TraitRepository.GetById(currentOriginId);
            var hasCert = _character.HasCertificationForOriginTrait(currentOriginId);
            ImGui.Text($"● {origin?.Name ?? currentOriginId}");
            if (hasCert) { ImGui.SameLine(); ImGui.TextColored(ColSuccess, "(certifié)"); }
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
            if (ImGui.Button("Retirer##origin_remove"))
            {
                _plugin.SetOriginTrait.Execute(_character, null);
                _plugin.RefreshEquippedTraits(_character);
            }
            ImGui.PopStyleColor(3);
        }
        else
        {
            ImGui.TextColored(ColMuted, "Aucun. Choisissez ci-dessous :");
            ImGui.Spacing();
            foreach (var trait in _plugin.TraitRepository.GetByCategory(TraitCategory.Origine))
            {
                var hasCert = _character.HasCertificationForOriginTrait(trait.Id);
                if (hasCert) ImGui.TextColored(ColSuccess, "★");
                else ImGui.TextColored(ColMuted, "○");
                ImGui.SameLine();
                if (ImGui.Button($"+ {trait.Name}##orig_{trait.Id}"))
                {
                    _plugin.SetOriginTrait.Execute(_character, trait.Id);
                    _plugin.RefreshEquippedTraits(_character);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(hasCert ? $"{trait.Description}\n✓ Certifié — gratuit." : trait.Description);
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
                {
                    _plugin.UnequipTrait.Execute(_character, traitId);
                    _plugin.RefreshEquippedTraits(_character);
                }
                ImGui.PopStyleColor(3);
                ImGui.SameLine();
                ImGui.Text(trait?.Name ?? traitId);
                if (ImGui.IsItemHovered() && trait != null) ImGui.SetTooltip(trait.Description);
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
            var categories = new[] { TraitCategory.Connaissance, TraitCategory.RoleDps, TraitCategory.RoleSoigneur, TraitCategory.RoleTank, TraitCategory.Job };
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
                    var hasConflict = trait.ExclusiveGroup != null &&
                        _character.EquippedTraitIds
                            .Select(id => _plugin.TraitRepository.GetById(id))
                            .Any(t => t?.ExclusiveGroup == trait.ExclusiveGroup);
                    var canEquip = !hasConflict;

                    ImGui.PushStyleColor(ImGuiCol.Button, canEquip ? new Vector4(0.09f, 0.37f, 0.65f, 0.20f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, canEquip ? new Vector4(0.09f, 0.37f, 0.65f, 0.40f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                    ImGui.PushStyleColor(ImGuiCol.Text, canEquip ? ColInfo : ColMuted);
                    if (ImGui.Button($"+ {trait.Name}##avail_{trait.Id}") && canEquip)
                    {
                        _plugin.EquipTrait.Execute(_character, trait.Id);
                        _plugin.RefreshEquippedTraits(_character);
                    }
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(trait.Description);
                }
                ImGui.Spacing();
            }
        }
        else
        {
            ImGui.TextColored(ColMuted, "Tous les slots de traits sont utilisés.");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawEditAbilities(rank);
    }

    private void DrawEditCertifications()
    {
        ImGui.TextColored(ColMuted, $"CERTIFICATIONS  ({_character.Certifications.Count})");
        ImGui.TextColored(ColMuted, "(accordées par un officier uniquement)");
        ImGui.Spacing();

        foreach (var cert in _character.Certifications.ToList())
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
            if (ImGui.Button($"✕##cert_rm_{cert.Id}"))
                _plugin.RemoveCertification.Execute(_character, cert.Id);
            ImGui.PopStyleColor(3);
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess);
            ImGui.Text("★");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.Text(cert.Name);

            if (cert.LinkedOriginTraitId != null)
            {
                var trait = _plugin.TraitRepository.GetById(cert.LinkedOriginTraitId);
                ImGui.SameLine();
                ImGui.TextColored(ColMuted, $"[Trait : {trait?.Name ?? cert.LinkedOriginTraitId}]");
            }
            if (cert.LinkedAbilityId != null && cert.FreePoints > 0)
            {
                var ability = _plugin.AbilityRepository.GetById(cert.LinkedAbilityId);
                ImGui.SameLine();
                ImGui.TextColored(ColMuted, $"[{cert.FreePoints} pt(s) : {ability?.Name ?? cert.LinkedAbilityId}]");
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("+ Ajouter une certification##cert_add"))
        {
            ImGui.Spacing();
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("Nom##cert_name", ref _newCertName, 128);

            ImGui.Spacing();
            ImGui.TextColored(ColMuted, "Trait d'origine lié (optionnel) :");
            ImGui.Spacing();

            var noOrigin = string.IsNullOrEmpty(_newCertOriginTraitId);
            if (noOrigin) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button("Aucun##cert_orig_none")) _newCertOriginTraitId = string.Empty;
            if (noOrigin) ImGui.PopStyleColor(2);
            ImGui.SameLine();

            foreach (var trait in _plugin.TraitRepository.GetByCategory(TraitCategory.Origine))
            {
                var selected = _newCertOriginTraitId == trait.Id;
                if (selected) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
                if (ImGui.Button(trait.Name + "##cert_orig_" + trait.Id))
                    _newCertOriginTraitId = selected ? string.Empty : trait.Id;
                if (selected) ImGui.PopStyleColor(2);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(trait.Description);
            }

            ImGui.Spacing();
            ImGui.TextColored(ColMuted, "Arme avec points gratuits (optionnel) :");
            ImGui.Spacing();

            var noAbility = string.IsNullOrEmpty(_newCertAbilityId);
            if (noAbility) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button("Aucune##cert_ab_none")) { _newCertAbilityId = string.Empty; _newCertFreePoints = 0; }
            if (noAbility) ImGui.PopStyleColor(2);
            ImGui.SameLine();

            foreach (var ability in _plugin.AbilityRepository.GetWeapons())
            {
                var selected = _newCertAbilityId == ability.Id;
                if (selected) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
                if (ImGui.Button(ability.Name + "##cert_ab_" + ability.Id))
                    _newCertAbilityId = selected ? string.Empty : ability.Id;
                if (selected) ImGui.PopStyleColor(2);
            }

            if (!string.IsNullOrEmpty(_newCertAbilityId))
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60);
                ImGui.InputInt("pts gratuits##cert_free", ref _newCertFreePoints, 1, 1);
                if (_newCertFreePoints < 0) _newCertFreePoints = 0;
            }

            ImGui.Spacing();
            var canAdd = !string.IsNullOrWhiteSpace(_newCertName);
            if (!canAdd) { ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.3f)); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.3f)); }
            if (ImGui.Button("✓ Ajouter##cert_confirm") && canAdd)
            {
                _plugin.AddCertification.Execute(
                    _character,
                    _newCertName,
                    string.IsNullOrEmpty(_newCertOriginTraitId) ? null : _newCertOriginTraitId,
                    string.IsNullOrEmpty(_newCertAbilityId) ? null : _newCertAbilityId,
                    _newCertFreePoints);
                _newCertName = string.Empty;
                _newCertOriginTraitId = string.Empty;
                _newCertAbilityId = string.Empty;
                _newCertFreePoints = 0;
            }
            if (!canAdd) ImGui.PopStyleColor(2);
            ImGui.Spacing();
        }
    }

    private void DrawEditAbilities(Rank rank)
    {
        ImGui.TextColored(ColMuted, "COMPÉTENCES");
        ImGui.Spacing();

        ImGui.TextColored(ColMuted, "Points accordés par le MJ :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(80);
        ImGui.InputInt("##skill_pts", ref _editSkillPoints, 1, 5);
        if (_editSkillPoints < 0) _editSkillPoints = 0;

        var remaining = Math.Max(0, _editSkillPoints - _character.SpentSkillPoints);
        ImGui.TextColored(ColMuted, $"Dépensés : {_character.SpentSkillPoints}  ·  Restants : {remaining}");
        ImGui.Spacing();

        // Apprises
        if (_character.EquippedAbilities.Count > 0)
        {
            ImGui.TextColored(ColMuted, "Apprises :");
            ImGui.Spacing();
            foreach (var equipped in _character.EquippedAbilities.ToList())
            {
                var ability = _plugin.AbilityRepository.GetById(equipped.AbilityId);
                if (ability is null) continue;
                var freePoints = _character.GetFreePointsForAbility(equipped.AbilityId);

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                if (ImGui.Button($"✕##ab_rm_{equipped.AbilityId}"))
                    _plugin.UnequipAbility.Execute(_character, equipped.AbilityId);
                ImGui.PopStyleColor(3);
                ImGui.SameLine();

                ImGui.Text(ability.Name);
                ImGui.SameLine();
                ImGui.TextColored(ColInfo, $"Lv{equipped.Level}");
                if (freePoints > 0) { ImGui.SameLine(); ImGui.TextColored(ColSuccess, $"({freePoints} pt(s) certif.)"); }

                if (equipped.Level < ability.MaxLevel)
                {
                    ImGui.SameLine();
                    var nextLevel = equipped.Level + 1;
                    var canLevelUp = rank.AllowsAbilityLevel(nextLevel) && remaining > 0;
                    ImGui.PushStyleColor(ImGuiCol.Button, canLevelUp ? new Vector4(0.09f, 0.37f, 0.65f, 0.20f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, canLevelUp ? new Vector4(0.09f, 0.37f, 0.65f, 0.40f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                    ImGui.PushStyleColor(ImGuiCol.Text, canLevelUp ? ColInfo : ColMuted);
                    if (ImGui.Button($"↑ Lv{nextLevel}##ab_up_{equipped.AbilityId}") && canLevelUp)
                        _plugin.EquipAbility.Execute(_character, equipped.AbilityId, nextLevel);
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered() && !canLevelUp)
                        ImGui.SetTooltip(!rank.AllowsAbilityLevel(nextLevel) ? "Rang insuffisant." : "Pas assez de points.");
                }

                var levelData = ability.Levels.FirstOrDefault(l => l.Level == equipped.Level);
                if (levelData != null)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ColMuted);
                    ImGui.TextWrapped(levelData.Description);
                    ImGui.PopStyleColor();
                }
                ImGui.Spacing();
            }
        }

        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(ColMuted, "Apprendre :");
        ImGui.Spacing();

        var abilityCategories = new[] { AbilityCategory.Weapon, AbilityCategory.RoleDps, AbilityCategory.RoleSoigneur, AbilityCategory.RoleTank, AbilityCategory.Job };
        foreach (var category in abilityCategories)
        {
            var available = _plugin.AbilityRepository.GetByCategory(category)
                .Where(a => _character.GetAbilityLevel(a.Id) == 0)
                .Where(a => a.RequiredJobId == null || a.RequiredJobId == _character.JobId)
                .ToList();
            if (available.Count == 0) continue;

            ImGui.TextColored(ColMuted, AbilityCategoryLabel(category));
            ImGui.Spacing();
            foreach (var ability in available)
            {
                var startLvl = ability.StartLevel;
                var freePoints = _character.GetFreePointsForAbility(ability.Id);
                var netCost = Math.Max(0, startLvl - freePoints);
                var canLearn = rank.AllowsAbilityLevel(startLvl) && (netCost == 0 || remaining > 0);

                ImGui.PushStyleColor(ImGuiCol.Button, canLearn ? new Vector4(0.09f, 0.37f, 0.65f, 0.20f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, canLearn ? new Vector4(0.09f, 0.37f, 0.65f, 0.40f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.Text, canLearn ? ColInfo : ColMuted);

                var label = freePoints > 0
                    ? $"+ {ability.Name} (Lv{startLvl} · {freePoints} pt(s) certif.)##ab_learn_{ability.Id}"
                    : $"+ {ability.Name} (Lv{startLvl})##ab_learn_{ability.Id}";

                if (ImGui.Button(label) && canLearn)
                    _plugin.EquipAbility.Execute(_character, ability.Id, startLvl);
                ImGui.PopStyleColor(3);

                if (ImGui.IsItemHovered())
                {
                    var desc = ability.Levels.FirstOrDefault(l => l.Level == startLvl)?.Description ?? "";
                    var tooltip = desc;
                    if (!rank.AllowsAbilityLevel(startLvl)) tooltip += "\n⚠ Rang insuffisant.";
                    else if (netCost > 0 && remaining <= 0) tooltip += "\n⚠ Pas assez de points.";
                    if (ability.UsageLimit != UsageLimit.None) tooltip += $"\n{UsageLimitLabel(ability.UsageLimit)}";
                    ImGui.SetTooltip(tooltip);
                }
            }
            ImGui.Spacing();
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

    private static string AbilityCategoryLabel(AbilityCategory category) => category switch
    {
        AbilityCategory.Weapon => "Armes",
        AbilityCategory.RoleDps => "Rôle — DPS",
        AbilityCategory.RoleSoigneur => "Rôle — Soigneur",
        AbilityCategory.RoleTank => "Rôle — Tank",
        AbilityCategory.Job => "Job",
        _ => category.ToString(),
    };

    private static string UsageLimitLabel(UsageLimit limit) => limit switch
    {
        UsageLimit.OncePerCombat => "⏱ 1× par combat",
        UsageLimit.TwicePerCombat => "⏱ 2× par combat",
        UsageLimit.OncePerEvent => "⏱ 1× par event",
        _ => "",
    };
}

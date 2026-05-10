using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Sts.Domain;
using STSPlugin.CharacterUseCases;
using Sts.Domain.UseCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Sts.Domain.Character;

namespace STSPlugin.Windows;

public class CharacterWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private Character _character;  

    private int _activeTab = 0;
    private bool _editMode = false;
    private string _editName = string.Empty;
    private int _editSkillPoints = 0;
    private string _editHistoire = string.Empty;

    private string _newCertName = string.Empty;
    private string _newCertOriginTraitId = string.Empty;
    private string _newCertAbilityId = string.Empty;
    private int _newCertFreePoints = 0;

    private string _newItemName = string.Empty;
    private string _newItemDescription = string.Empty;
    private ItemCategory _newItemCategory = ItemCategory.Item;
    private string _newItemAbilityId = string.Empty;

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

        ImGui.PushStyleColor(ImGuiCol.Button, _activeTab == 0 ? new Vector4(0.09f, 0.37f, 0.65f, 0.40f) : new Vector4(0.2f, 0.2f, 0.2f, 0.30f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
        if (ImGui.Button("Fiche##nav_sheet")) _activeTab = 0;
        ImGui.PopStyleColor(2);
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, _activeTab == 1 ? new Vector4(0.09f, 0.37f, 0.65f, 0.40f) : new Vector4(0.2f, 0.2f, 0.2f, 0.30f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
        if (ImGui.Button($"Inventaire ({_character.Inventory.Count})##nav_inv")) { _activeTab = 1; _editMode = false; }
        ImGui.PopStyleColor(2);

        ImGui.Separator();
        ImGui.Spacing();

        if (_activeTab == 0) { if (_editMode) DrawEditMode(); else DrawReadMode(); }
        else DrawInventoryTab();
    }

    /// <summary>
    /// Met à jour l'objet character avec les données fraîches de l'API.
    /// Appelé par Plugin.RefreshCharacterWindows après un sync.
    /// Si l'édition est en cours, la mise à jour est ignorée.
    /// </summary>
    public void UpdateCharacter(Character fresh)
    {
        if (_editMode) return;

        _character.Name = fresh.Name;
        _character.RankKey = fresh.RankKey;
        _character.Race = fresh.Race;
        _character.JobId = fresh.JobId;
        _character.Histoire = fresh.Histoire;
        _character.ReputationLevel = fresh.ReputationLevel;
        _character.SkillPoints = fresh.SkillPoints;
        _character.OriginTraitId = fresh.OriginTraitId;
        _character.EquippedTraitIds = fresh.EquippedTraitIds;
        _character.EquippedAbilities = fresh.EquippedAbilities;
        _character.Certifications = fresh.Certifications;
        _character.Inventory = fresh.Inventory;
        _character.MainHandItemId = fresh.MainHandItemId;
        _character.OffHandItemId = fresh.OffHandItemId;
        _character.QuickbarActionIds = fresh.QuickbarActionIds;

        WindowName = $"{_character.Name} — Fiche STS##{_character.Id}";
    }

    // ------------------------------------------------------------------ En-tête

    private void DrawHeader()
    {
        var isActive = _plugin.Configuration.ActiveCharacterId == _character.Id;
        if (isActive)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess); ImGui.Text("●"); ImGui.PopStyleColor();
            ImGui.SameLine(); ImGui.TextColored(ColMuted, "Personnage actif");
        }
        else
        {
            ImGui.TextColored(ColMuted, "○ Inactif"); ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.09f, 0.37f, 0.65f, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColInfo);
            if (ImGui.Button("Activer##hdr_activate"))
            {
                _plugin.SetActiveCharacter.Execute(_character);
                _plugin.RefreshEquippedTraits();
            }
            ImGui.PopStyleColor(3);
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - 160);

        if (_editMode)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.06f, 0.43f, 0.34f, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.06f, 0.43f, 0.34f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess);
            if (ImGui.Button("✓ Sauver##save"))
            {
                if (!string.IsNullOrWhiteSpace(_editName)) _character.Name = _editName.Trim();
                _character.Histoire = _editHistoire;
                _character.SkillPoints = Math.Max(0, _editSkillPoints); // inliné — UpdateCharacter persiste tout
                _ = Task.Run(() => _plugin.UpdateCharacter.ExecuteAsync(_character));
                WindowName = $"{_character.Name} — Fiche STS##{_character.Id}";
                _editMode = false;
            }
            ImGui.PopStyleColor(3);
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.40f));
            if (ImGui.Button("✕ Annuler##cancel")) _editMode = false;
            ImGui.PopStyleColor(2);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.36f, 0.19f, 0.58f, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.36f, 0.19f, 0.58f, 0.45f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.78f, 0.60f, 1.0f, 1f));
            if (ImGui.Button("📋 Discord##export")) ImGui.SetClipboardText(BuildDiscordExport());
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Copier la fiche au format Discord");
            ImGui.SameLine();

            if (ImGui.Button("✎ Éditer##edit"))
            {
                _editName = _character.Name;
                _editHistoire = _character.Histoire;
                _editSkillPoints = _character.SkillPoints;
                _editMode = true;
            }
        }
    }

    // ------------------------------------------------------------------ Export Discord

    private string BuildDiscordExport()
    {
        var rank = Rank.Get(_character.RankKey);
        var job = _character.JobId != null ? _plugin.JobRepository.GetById(_character.JobId) : null;
        var sb = new StringBuilder();

        sb.AppendLine("```");
        sb.AppendLine($"{_character.Race.Label()} - {job?.Name ?? "Sans classe"}");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("```");
        sb.AppendLine($"**Rang :** {rank.Label}");
        sb.AppendLine($"**Réussite :** {rank.Palier}+");
        sb.AppendLine($"**Rerolls :** {rank.Rerolls}");
        sb.AppendLine($"**Réputation :** {Reputation.GetLabel(_character.ReputationLevel)} ({(_character.ReputationLevel >= 0 ? "+" : "")}{_character.ReputationLevel})");
        sb.AppendLine();
        sb.AppendLine($"**Histoire :** {(string.IsNullOrWhiteSpace(_character.Histoire) ? "..." : _character.Histoire.Trim())}");
        sb.AppendLine("```");
        sb.AppendLine();

        var certByTrait = _character.Certifications.Where(c => c.LinkedOriginTraitId != null).ToDictionary(c => c.LinkedOriginTraitId!, c => c);
        var certByAbility = _character.Certifications.Where(c => c.LinkedAbilityId != null).ToDictionary(c => c.LinkedAbilityId!, c => c);

        sb.AppendLine("## Capacités :");
        if (_character.EquippedAbilities.Count == 0) { sb.AppendLine("- Aucune"); }
        else
        {
            foreach (var eq in _character.EquippedAbilities)
            {
                var ab = _plugin.AbilityRepository.GetById(eq.AbilityId); if (ab is null) continue;
                sb.Append($"- **{ab.Name} Lv{eq.Level}**");
                if (certByAbility.TryGetValue(eq.AbilityId, out var abCert) && abCert.FreePoints > 0)
                    sb.Append($" *(★ {abCert.Name} — {abCert.FreePoints} pt(s) gratuit(s))*");
                sb.AppendLine(" :");
                if (ab.UsageLimit != UsageLimit.None) sb.AppendLine($"> {UsageLimitLabel(ab.UsageLimit)}");
                foreach (var ld in ab.Levels.Where(l => l.Level <= eq.Level).OrderBy(l => l.Level))
                {
                    if (string.IsNullOrWhiteSpace(ld.Description)) continue;
                    sb.AppendLine($"> Rang {ld.Level} : {ld.Description.Trim()}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Traits :");
        var allTraits = _character.EquippedTraitIds.ToList();
        if (_character.OriginTraitId != null) allTraits.Insert(0, _character.OriginTraitId);
        if (allTraits.Count == 0) { sb.AppendLine("- Aucun"); }
        else
        {
            foreach (var tid in allTraits)
            {
                var t = _plugin.TraitRepository.GetById(tid);
                if (t is null) { sb.AppendLine($"- **{tid}**"); continue; }
                sb.Append($"- **{t.Name}**");
                if (certByTrait.TryGetValue(tid, out var traitCert)) sb.Append($" *(★ {traitCert.Name} — gratuit)*");
                sb.AppendLine(" :");
                if (!string.IsNullOrWhiteSpace(t.Description)) sb.AppendLine($"> {t.Description.Trim()}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Certifications :");
        if (_character.Certifications.Count == 0) { sb.AppendLine("- Aucune"); }
        else
        {
            foreach (var cert in _character.Certifications)
            {
                sb.AppendLine($"- **{cert.Name}**");
                if (cert.LinkedOriginTraitId != null) { var t = _plugin.TraitRepository.GetById(cert.LinkedOriginTraitId); sb.AppendLine($"> Trait d'origine gratuit : {t?.Name ?? cert.LinkedOriginTraitId}"); }
                if (cert.LinkedAbilityId != null && cert.FreePoints > 0) { var a = _plugin.AbilityRepository.GetById(cert.LinkedAbilityId); sb.AppendLine($"> {cert.FreePoints} pt(s) gratuit(s) sur : {a?.Name ?? cert.LinkedAbilityId}"); }
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Inventaire :");
        if (_character.Inventory.Count == 0) { sb.AppendLine("- Aucun objet."); }
        else
        {
            var weapons = _character.Inventory.Where(i => i.Category == ItemCategory.Weapon).OrderBy(i => i.SortIndex).ToList();
            var items = _character.Inventory.Where(i => i.Category == ItemCategory.Item).OrderBy(i => i.SortIndex).ToList();
            if (weapons.Count > 0)
            {
                sb.AppendLine("**Armes :**");
                foreach (var w in weapons)
                {
                    var slots = new List<string>();
                    if (_character.MainHandItemId == w.Id) slots.Add("main principale");
                    if (_character.OffHandItemId == w.Id) slots.Add("main secondaire");
                    var equippedNote = slots.Count > 0 ? $" *(équipée — {string.Join(", ", slots)})*" : "";
                    var masteredNote = _character.IsWeaponUnmastered(w) ? " *(non maîtrisée — palier 8)*" : "";
                    var linkedAbility = w.LinkedAbilityId != null ? _plugin.AbilityRepository.GetById(w.LinkedAbilityId) : null;
                    var linkedNote = linkedAbility != null ? $" — compétence : {linkedAbility.Name}" : "";
                    sb.AppendLine($"- **{w.Name}**{equippedNote}{masteredNote}{linkedNote}");
                    if (!string.IsNullOrWhiteSpace(w.Description)) sb.AppendLine($"> {w.Description.Trim()}");
                }
            }
            if (items.Count > 0)
            {
                if (weapons.Count > 0) sb.AppendLine();
                sb.AppendLine("**Objets :**");
                foreach (var item in items)
                {
                    sb.AppendLine($"- **{item.Name}**");
                    if (!string.IsNullOrWhiteSpace(item.Description)) sb.AppendLine($"> {item.Description.Trim()}");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    // ================================================================== Inventaire

    private void DrawInventoryTab()
    {
        var weapons = _character.Inventory.Where(i => i.Category == ItemCategory.Weapon).OrderBy(i => i.SortIndex).ToList();
        var items = _character.Inventory.Where(i => i.Category == ItemCategory.Item).OrderBy(i => i.SortIndex).ToList();

        ImGui.TextColored(ColMuted, "EMPLACEMENTS"); ImGui.Spacing();

        var main = _character.MainHandItemId != null ? _character.Inventory.FirstOrDefault(i => i.Id == _character.MainHandItemId) : null;
        var off = _character.OffHandItemId != null ? _character.Inventory.FirstOrDefault(i => i.Id == _character.OffHandItemId) : null;

        ImGui.TextColored(ColMuted, "Main principale :"); ImGui.SameLine();
        if (main != null)
        {
            var mastered = !_character.IsWeaponUnmastered(main);
            ImGui.TextColored(mastered ? ColSuccess : ColWarn, main.Name);
            if (!mastered) { ImGui.SameLine(); ImGui.TextColored(ColWarn, "⚠ palier 8"); }
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.40f));
            if (ImGui.Button("Retirer##main_unequip"))
                _ = Task.Run(() => _plugin.SetItemSlot.ExecuteAsync(_character, EquipSlot.MainHand, null));
            ImGui.PopStyleColor(2);
        }
        else ImGui.TextColored(ColMuted, "—");

        ImGui.TextColored(ColMuted, "Main secondaire :"); ImGui.SameLine();
        if (off != null)
        {
            var mastered = !_character.IsWeaponUnmastered(off);
            ImGui.TextColored(mastered ? ColSuccess : ColWarn, off.Name);
            if (!mastered) { ImGui.SameLine(); ImGui.TextColored(ColWarn, "⚠ palier 8"); }
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.40f));
            if (ImGui.Button("Retirer##off_unequip"))
                _ = Task.Run(() => _plugin.SetItemSlot.ExecuteAsync(_character, EquipSlot.OffHand, null));
            ImGui.PopStyleColor(2);
        }
        else ImGui.TextColored(ColMuted, "—");

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(ColMuted, $"INVENTAIRE  ({_character.Inventory.Count} objet(s))"); ImGui.Spacing();

        if (weapons.Count > 0)
        {
            ImGui.TextColored(ColMuted, "Armes :"); ImGui.Spacing();
            foreach (var w in weapons)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                if (ImGui.Button($"✕##inv_rm_{w.Id}"))
                    _ = Task.Run(() => _plugin.RemoveInventoryItem.ExecuteAsync(_character, w.Id));
                ImGui.PopStyleColor(3); ImGui.SameLine();

                var isMain = _character.MainHandItemId == w.Id;
                ImGui.PushStyleColor(ImGuiCol.Button, isMain ? new Vector4(0.06f, 0.43f, 0.34f, 0.40f) : new Vector4(0.09f, 0.37f, 0.65f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, isMain ? ColSuccess : ColInfo);
                if (ImGui.Button($"M##inv_main_{w.Id}"))
                    _ = Task.Run(() => _plugin.SetItemSlot.ExecuteAsync(_character, EquipSlot.MainHand, isMain ? null : w.Id));
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(isMain ? "Retirer de la main principale" : "Équiper en main principale");
                ImGui.SameLine();

                var isOff = _character.OffHandItemId == w.Id;
                ImGui.PushStyleColor(ImGuiCol.Button, isOff ? new Vector4(0.06f, 0.43f, 0.34f, 0.40f) : new Vector4(0.09f, 0.37f, 0.65f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, isOff ? ColSuccess : ColInfo);
                if (ImGui.Button($"S##inv_off_{w.Id}"))
                    _ = Task.Run(() => _plugin.SetItemSlot.ExecuteAsync(_character, EquipSlot.OffHand, isOff ? null : w.Id));
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(isOff ? "Retirer de la main secondaire" : "Équiper en main secondaire");
                ImGui.SameLine();

                var mastered = !_character.IsWeaponUnmastered(w);
                ImGui.TextColored(mastered ? ColSuccess : ColWarn, w.Name);
                if (ImGui.IsItemHovered() && !string.IsNullOrWhiteSpace(w.Description)) ImGui.SetTooltip(w.Description);
                var sn = w.LinkedAbilityId != null ? _plugin.AbilityRepository.GetById(w.LinkedAbilityId)?.Name : null;
                if (sn != null) { ImGui.SameLine(); ImGui.TextColored(ColMuted, $"[{sn}]"); }
                if (!mastered) { ImGui.SameLine(); ImGui.TextColored(ColWarn, "⚠ palier 8"); }
            }
            ImGui.Spacing();
        }

        if (items.Count > 0)
        {
            ImGui.TextColored(ColMuted, "Objets :"); ImGui.Spacing();
            foreach (var item in items)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                if (ImGui.Button($"✕##inv_rm_{item.Id}"))
                    _ = Task.Run(() => _plugin.RemoveInventoryItem.ExecuteAsync(_character, item.Id));
                ImGui.PopStyleColor(3); ImGui.SameLine();
                ImGui.Text(item.Name);
                if (ImGui.IsItemHovered() && !string.IsNullOrWhiteSpace(item.Description)) ImGui.SetTooltip(item.Description);
            }
            ImGui.Spacing();
        }

        if (_character.Inventory.Count == 0) ImGui.TextColored(ColMuted, "Inventaire vide.");
        ImGui.Separator(); ImGui.Spacing();

        if (ImGui.CollapsingHeader("+ Ajouter un objet##inv_add"))
        {
            ImGui.Spacing();
            ImGui.TextColored(ColMuted, "Type :"); ImGui.SameLine();
            var isWeapon = _newItemCategory == ItemCategory.Weapon;
            if (!isWeapon) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button("Objet##inv_type_item")) { _newItemCategory = ItemCategory.Item; _newItemAbilityId = string.Empty; }
            if (!isWeapon) ImGui.PopStyleColor(2); ImGui.SameLine();
            if (isWeapon) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button("Arme##inv_type_weapon")) _newItemCategory = ItemCategory.Weapon;
            if (isWeapon) ImGui.PopStyleColor(2);

            ImGui.Spacing();
            ImGui.SetNextItemWidth(200); ImGui.InputText("Nom##inv_name", ref _newItemName, 128);
            ImGui.Spacing(); ImGui.TextColored(ColMuted, "Description :");
            ImGui.SetNextItemWidth(-1); ImGui.InputTextMultiline("##inv_desc", ref _newItemDescription, 4096, new Vector2(0, 80));

            if (_newItemCategory == ItemCategory.Weapon)
            {
                ImGui.Spacing(); ImGui.TextColored(ColMuted, "Compétence d'arme liée (optionnel) :"); ImGui.Spacing();
                var noAb = string.IsNullOrEmpty(_newItemAbilityId);
                if (noAb) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
                if (ImGui.Button("Aucune##inv_ab_none")) _newItemAbilityId = string.Empty;
                if (noAb) ImGui.PopStyleColor(2); ImGui.SameLine();
                foreach (var ab in _plugin.AbilityRepository.GetWeapons())
                {
                    var sel = _newItemAbilityId == ab.Id;
                    if (sel) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
                    if (ImGui.Button(ab.Name + "##inv_ab_" + ab.Id)) _newItemAbilityId = sel ? string.Empty : ab.Id;
                    if (sel) ImGui.PopStyleColor(2);
                }
                if (!string.IsNullOrEmpty(_newItemAbilityId))
                {
                    var lvl = _character.GetAbilityLevel(_newItemAbilityId); ImGui.Spacing();
                    if (lvl == 0) ImGui.TextColored(ColWarn, "⚠ Non maîtrisée — palier d'attaque passera à 8.");
                    else ImGui.TextColored(ColSuccess, $"✓ Maîtrisée (Lv{lvl}).");
                }
            }

            ImGui.Spacing();
            var canAdd = !string.IsNullOrWhiteSpace(_newItemName);
            if (!canAdd) { ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.3f)); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.3f)); }
            if (ImGui.Button("✓ Ajouter##inv_confirm") && canAdd)
            {
                var itemName = _newItemName;
                var itemDesc = _newItemDescription;
                var itemCat = _newItemCategory;
                var itemAbId = string.IsNullOrEmpty(_newItemAbilityId) ? null : _newItemAbilityId;
                _newItemName = _newItemDescription = _newItemAbilityId = string.Empty;
                _newItemCategory = ItemCategory.Item;
                _ = Task.Run(() => _plugin.AddInventoryItem.ExecuteAsync(_character, itemName, itemDesc, itemCat, itemAbId));
            }
            if (!canAdd) ImGui.PopStyleColor(2);
            ImGui.Spacing();
        }
    }

    // ================================================================== Mode lecture

    private void DrawReadMode()
    {
        var rank = Rank.Get(_character.RankKey);
        var job = _character.JobId != null ? _plugin.JobRepository.GetById(_character.JobId) : null;

        ImGui.Text(_character.Name); ImGui.SameLine();
        ImGui.TextColored(ColMuted, $"— {_character.Race.Label()}  ·  {rank.Label}  ·  palier {rank.Palier}+  ·  {rank.Rerolls} reroll(s)  ·  {rank.Traits} traits");
        ImGui.Spacing();
        ImGui.TextColored(ColMuted, "Job :"); ImGui.SameLine(); ImGui.Text(job?.Name ?? "Aucun");
        ImGui.TextColored(ColMuted, "Réputation :"); ImGui.SameLine();
        var repColor = _character.ReputationLevel < 0 ? ColDanger : _character.ReputationLevel >= 6 ? ColSuccess : ColMuted;
        ImGui.TextColored(repColor, $"{Reputation.GetLabel(_character.ReputationLevel)}  ({(_character.ReputationLevel >= 0 ? "+" : "")}{_character.ReputationLevel})");

        if (!string.IsNullOrWhiteSpace(_character.Histoire))
        {
            ImGui.Spacing(); ImGui.TextColored(ColMuted, "Histoire :");
            ImGui.PushStyleColor(ImGuiCol.Text, ColMuted); ImGui.TextWrapped(_character.Histoire); ImGui.PopStyleColor();
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        DrawReadCertifications(); ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        ImGui.TextColored(ColMuted, "TRAIT D'ORIGINE"); ImGui.Spacing();
        if (_character.OriginTraitId is { } oid)
        {
            var origin = _plugin.TraitRepository.GetById(oid); var hc = _character.HasCertificationForOriginTrait(oid);
            if (origin != null)
            {
                ImGui.Text($"● {origin.Name}");
                if (hc) { ImGui.SameLine(); ImGui.TextColored(ColSuccess, "(certifié — gratuit)"); }
                ImGui.PushStyleColor(ImGuiCol.Text, ColMuted); ImGui.TextWrapped(origin.Description); ImGui.PopStyleColor();
            }
        }
        else ImGui.TextColored(ColMuted, "Aucun trait d'origine.");
        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        ImGui.TextColored(ColMuted, $"TRAITS  ({_character.EquippedTraitIds.Count}/{rank.Traits})"); ImGui.Spacing();
        if (_character.EquippedTraitIds.Count == 0) ImGui.TextColored(ColMuted, "Aucun trait équipé.");
        else foreach (var tid in _character.EquippedTraitIds)
        {
            var t = _plugin.TraitRepository.GetById(tid); if (t is null) continue;
            ImGui.Text($"● {t.Name}"); ImGui.PushStyleColor(ImGuiCol.Text, ColMuted); ImGui.TextWrapped(t.Description); ImGui.PopStyleColor(); ImGui.Spacing();
            if (t.UsageLimit != UsageLimit.None) { ImGui.SameLine(); ImGui.TextColored(ColWarn, UsageLimitLabel(t.UsageLimit)); }
        }
        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        DrawReadAbilities();
    }

    private void DrawReadCertifications()
    {
        ImGui.TextColored(ColMuted, $"CERTIFICATIONS  ({_character.Certifications.Count})"); ImGui.Spacing();
        if (_character.Certifications.Count == 0) { ImGui.TextColored(ColMuted, "Aucune certification."); return; }
        foreach (var cert in _character.Certifications)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess); ImGui.Text("★"); ImGui.PopStyleColor(); ImGui.SameLine(); ImGui.Text(cert.Name);
            if (cert.LinkedOriginTraitId != null) { var t = _plugin.TraitRepository.GetById(cert.LinkedOriginTraitId); ImGui.SameLine(); ImGui.TextColored(ColMuted, $"→ Trait : {t?.Name ?? cert.LinkedOriginTraitId}"); }
            if (cert.LinkedAbilityId != null && cert.FreePoints > 0) { var a = _plugin.AbilityRepository.GetById(cert.LinkedAbilityId); ImGui.SameLine(); ImGui.TextColored(ColMuted, $"→ {cert.FreePoints} pt(s) : {a?.Name ?? cert.LinkedAbilityId}"); }
        }
    }

    private void DrawReadAbilities()
    {
        ImGui.TextColored(ColMuted, $"COMPÉTENCES  (points : {_character.SpentSkillPoints} / {_character.SkillPoints})"); ImGui.Spacing();
        if (_character.EquippedAbilities.Count == 0) { ImGui.TextColored(ColMuted, "Aucune compétence apprise."); return; }
        foreach (var eq in _character.EquippedAbilities)
        {
            var ab = _plugin.AbilityRepository.GetById(eq.AbilityId); if (ab is null) continue;
            var fp = _character.GetFreePointsForAbility(eq.AbilityId);
            ImGui.Text($"● {ab.Name}"); ImGui.SameLine(); ImGui.TextColored(ColInfo, $"Lv{eq.Level}");
            if (fp > 0) { ImGui.SameLine(); ImGui.TextColored(ColSuccess, $"({fp} pt(s) certif.)"); }
            if (ab.UsageLimit != UsageLimit.None) { ImGui.SameLine(); ImGui.TextColored(ColWarn, UsageLimitLabel(ab.UsageLimit)); }
            foreach (var ld in ab.Levels.Where(l => l.Level <= eq.Level).OrderBy(l => l.Level))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ColMuted);
                if (eq.Level > 1) ImGui.TextColored(ColInfo, $"  Lv{ld.Level} :");
                ImGui.TextWrapped(ld.Description);
                ImGui.PopStyleColor();
            }
            ImGui.Spacing();
        }
    }

    // ================================================================== Mode édition

    private void DrawEditMode()
    {
        var rank = Rank.Get(_character.RankKey);

        ImGui.TextColored(ColMuted, "Nom :"); ImGui.SameLine();
        ImGui.SetNextItemWidth(200); ImGui.InputText("##edit_name", ref _editName, 64); ImGui.Spacing();

        ImGui.TextColored(ColMuted, "Race :"); ImGui.SameLine();
        ImGui.SetNextItemWidth(180);
        if (ImGui.BeginCombo("##race_combo", _character.Race.Label()))
        {
            foreach (var race in Enum.GetValues<CharacterRace>())
            {
                var selected = _character.Race == race;
                if (ImGui.Selectable(race.Label() + "##race_sel_" + race, selected)) _character.Race = race;
                if (selected) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        ImGui.Spacing();

        ImGui.TextColored(ColMuted, "Rang :"); ImGui.Spacing();
        foreach (var rk in Enum.GetValues<RankKey>())
        {
            var cur = _character.RankKey == rk;
            if (cur) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button(Rank.Get(rk).Label + "##rk_" + rk))
            {
                _character.RankKey = rk;
                if (_plugin.Configuration.ActiveCharacterId == _character.Id) _plugin.Engine.ChangeRank(rk);
            }
            if (cur) ImGui.PopStyleColor(2); ImGui.SameLine();
        }
        ImGui.NewLine(); ImGui.Spacing();

        ImGui.TextColored(ColMuted, "Réputation :"); ImGui.SameLine();
        ImGui.Text($"{Reputation.GetLabel(_character.ReputationLevel)}  ({(_character.ReputationLevel >= 0 ? "+" : "")}{_character.ReputationLevel})");
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.25f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.40f));
        if (ImGui.Button("−##rep_dec") && _character.ReputationLevel > Reputation.Min)
            _character.ReputationLevel = Reputation.Clamp(_character.ReputationLevel - 1);
        ImGui.SameLine();
        if (ImGui.Button("+##rep_inc") && _character.ReputationLevel < Reputation.Max)
            _character.ReputationLevel = Reputation.Clamp(_character.ReputationLevel + 1);
        ImGui.PopStyleColor(2);
        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        ImGui.TextColored(ColMuted, "Job :"); ImGui.Spacing();
        var allJobs = _plugin.JobRepository.GetAll();
        var jobLabel = _character.JobId != null
            ? allJobs.FirstOrDefault(j => j.Id == _character.JobId)?.Name ?? _character.JobId
            : "— Aucun —";
        ImGui.SetNextItemWidth(220);
        if (ImGui.BeginCombo("##job_combo", jobLabel))
        {
            var noJob = _character.JobId == null;
            if (ImGui.Selectable("— Aucun —", noJob))
                _ = Task.Run(() => _plugin.SetJob.ExecuteAsync(_character, null));
            if (noJob) ImGui.SetItemDefaultFocus();
            ImGui.Separator();
            foreach (var j in allJobs)
            {
                var selected = _character.JobId == j.Id;
                if (ImGui.Selectable(j.Name + "##job_sel_" + j.Id, selected))
                    _ = Task.Run(() => _plugin.SetJob.ExecuteAsync(_character, j.Id));
                if (selected) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        ImGui.TextColored(ColMuted, "Histoire :"); ImGui.Spacing();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextMultiline("##edit_histoire", ref _editHistoire, 4096, new Vector2(0, 80));
        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        DrawEditCertifications(); ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        ImGui.TextColored(ColMuted, "TRAIT D'ORIGINE");
        ImGui.TextColored(ColMuted, "(gratuit si certifié, sinon nécessite la certification MJ)"); ImGui.Spacing();
        if (_character.OriginTraitId is { } coid)
        {
            var origin = _plugin.TraitRepository.GetById(coid); var hc = _character.HasCertificationForOriginTrait(coid);
            ImGui.Text($"● {origin?.Name ?? coid}"); if (hc) { ImGui.SameLine(); ImGui.TextColored(ColSuccess, "(certifié)"); }
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
            if (ImGui.Button("Retirer##origin_remove"))
                _ = Task.Run(async () => { await _plugin.SetOriginTrait.ExecuteAsync(_character, null); _plugin.RefreshEquippedTraits(_character); });
            ImGui.PopStyleColor(3);
        }
        else
        {
            ImGui.TextColored(ColMuted, "Aucun. Choisissez ci-dessous :"); ImGui.Spacing();
            foreach (var t in _plugin.TraitRepository.GetByCategory(TraitCategory.Origine))
            {
                var hc = _character.HasCertificationForOriginTrait(t.Id);
                if (hc) ImGui.TextColored(ColSuccess, "★"); else ImGui.TextColored(ColMuted, "○"); ImGui.SameLine();
                if (ImGui.Button($"+ {t.Name}##orig_{t.Id}"))
                    _ = Task.Run(async () => { await _plugin.SetOriginTrait.ExecuteAsync(_character, t.Id); _plugin.RefreshEquippedTraits(_character); });
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(hc ? $"{t.Description}\n✓ Certifié — gratuit." : t.Description);
            }
        }
        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        ImGui.TextColored(ColMuted, $"TRAITS ÉQUIPÉS  ({_character.EquippedTraitIds.Count}/{rank.Traits})"); ImGui.Spacing();
        if (_character.EquippedTraitIds.Count == 0) ImGui.TextColored(ColMuted, "Aucun trait équipé.");
        else foreach (var tid in _character.EquippedTraitIds.ToList())
        {
            var t = _plugin.TraitRepository.GetById(tid);
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
            if (ImGui.Button($"✕##remove_{tid}"))
                _ = Task.Run(async () => { await _plugin.UnequipTrait.ExecuteAsync(_character, tid); _plugin.RefreshEquippedTraits(_character); });
            ImGui.PopStyleColor(3); ImGui.SameLine(); ImGui.Text(t?.Name ?? tid);
            if (ImGui.IsItemHovered() && t != null) ImGui.SetTooltip(t.Description);
        }
        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        if (_character.FreeTraitSlots > 0)
        {
            ImGui.TextColored(ColMuted, $"TRAITS DISPONIBLES  ({_character.FreeTraitSlots} slot(s) libre(s))"); ImGui.Spacing();
            var cats = new[] { TraitCategory.Connaissance, TraitCategory.RoleDps, TraitCategory.RoleSoigneur, TraitCategory.RoleTank, TraitCategory.Job };
            foreach (var cat in cats)
            {
                var avail = _plugin.TraitRepository.GetByCategory(cat)
                    .Where(t => !_character.HasTrait(t.Id))
                    .Where(t => t.RequiredJobIds == null || t.RequiredJobIds.Count == 0 ||
                                (_character.JobId != null && t.RequiredJobIds.Contains(_character.JobId)))
                    .ToList();
                if (avail.Count == 0) continue;
                ImGui.TextColored(ColMuted, CategoryLabel(cat)); ImGui.Spacing();
                foreach (var t in avail)
                {
                    var conflict = t.ExclusiveGroup != null && _character.EquippedTraitIds
                        .Select(id => _plugin.TraitRepository.GetById(id))
                        .Any(x => x?.ExclusiveGroup == t.ExclusiveGroup);
                    ImGui.PushStyleColor(ImGuiCol.Button, !conflict ? new Vector4(0.09f, 0.37f, 0.65f, 0.20f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, !conflict ? new Vector4(0.09f, 0.37f, 0.65f, 0.40f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                    ImGui.PushStyleColor(ImGuiCol.Text, !conflict ? ColInfo : ColMuted);
                    if (ImGui.Button($"+ {t.Name}##avail_{t.Id}") && !conflict)
                        _ = Task.Run(async () => { await _plugin.EquipTrait.ExecuteAsync(_character, t.Id); _plugin.RefreshEquippedTraits(_character); });
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(t.Description);
                }
                ImGui.Spacing();
            }
        }
        else ImGui.TextColored(ColMuted, "Tous les slots de traits sont utilisés.");
        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        DrawEditAbilities(rank);
    }

    private void DrawEditCertifications()
    {
        ImGui.TextColored(ColMuted, $"CERTIFICATIONS  ({_character.Certifications.Count})");
        ImGui.TextColored(ColMuted, "(accordées par un officier uniquement)"); ImGui.Spacing();

        foreach (var cert in _character.Certifications.ToList())
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
            if (ImGui.Button($"✕##cert_rm_{cert.Id}"))
                _ = Task.Run(() => _plugin.RemoveCertification.ExecuteAsync(_character, cert.Id));
            ImGui.PopStyleColor(3); ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColSuccess); ImGui.Text("★"); ImGui.PopStyleColor(); ImGui.SameLine(); ImGui.Text(cert.Name);
            if (cert.LinkedOriginTraitId != null) { var t = _plugin.TraitRepository.GetById(cert.LinkedOriginTraitId); ImGui.SameLine(); ImGui.TextColored(ColMuted, $"[Trait : {t?.Name ?? cert.LinkedOriginTraitId}]"); }
            if (cert.LinkedAbilityId != null && cert.FreePoints > 0) { var a = _plugin.AbilityRepository.GetById(cert.LinkedAbilityId); ImGui.SameLine(); ImGui.TextColored(ColMuted, $"[{cert.FreePoints} pt(s) : {a?.Name ?? cert.LinkedAbilityId}]"); }
        }

        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.09f, 0.37f, 0.65f, 0.20f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.09f, 0.37f, 0.65f, 0.40f));
        ImGui.PushStyleColor(ImGuiCol.Text, ColInfo);
        if (ImGui.Button("+ Ajouter une certification##cert_open"))
        {
            _newCertName = _newCertOriginTraitId = _newCertAbilityId = string.Empty;
            _newCertFreePoints = 0;
            ImGui.OpenPopup("Nouvelle certification##cert_modal");
        }
        ImGui.PopStyleColor(3);
        DrawCertificationPopup();
    }

    private void DrawCertificationPopup()
    {
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(520, 0), ImGuiCond.Appearing);

        if (!ImGui.BeginPopupModal("Nouvelle certification##cert_modal", ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextColored(ColMuted, "NOM"); ImGui.Spacing();
        ImGui.SetNextItemWidth(300); ImGui.InputText("##cert_name", ref _newCertName, 128);
        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(ColMuted, "Trait d'origine lié (optionnel) :"); ImGui.Spacing();

        var noOr = string.IsNullOrEmpty(_newCertOriginTraitId);
        if (noOr) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
        if (ImGui.Button("Aucun##cert_orig_none")) _newCertOriginTraitId = string.Empty;
        if (noOr) ImGui.PopStyleColor(2); ImGui.SameLine();
        foreach (var t in _plugin.TraitRepository.GetByCategory(TraitCategory.Origine))
        {
            var sel = _newCertOriginTraitId == t.Id;
            if (sel) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button(t.Name + "##cert_orig_" + t.Id)) _newCertOriginTraitId = sel ? string.Empty : t.Id;
            if (sel) ImGui.PopStyleColor(2);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(t.Description);
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(ColMuted, "Compétence d'arme avec points gratuits (optionnel) :"); ImGui.Spacing();

        var noAb = string.IsNullOrEmpty(_newCertAbilityId);
        if (noAb) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
        if (ImGui.Button("Aucune##cert_ab_none")) { _newCertAbilityId = string.Empty; _newCertFreePoints = 0; }
        if (noAb) ImGui.PopStyleColor(2); ImGui.SameLine();
        foreach (var a in _plugin.AbilityRepository.GetWeapons())
        {
            var sel = _newCertAbilityId == a.Id;
            if (sel) { ImGui.PushStyleColor(ImGuiCol.Button, ColActive); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColActive); }
            if (ImGui.Button(a.Name + "##cert_ab_" + a.Id)) _newCertAbilityId = sel ? string.Empty : a.Id;
            if (sel) ImGui.PopStyleColor(2);
        }
        if (!string.IsNullOrEmpty(_newCertAbilityId))
        {
            ImGui.Spacing();
            ImGui.SetNextItemWidth(140);
            ImGui.InputInt("pts gratuits##cert_free", ref _newCertFreePoints, 1, 1);
            if (_newCertFreePoints < 0) _newCertFreePoints = 0;
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        var canAdd = !string.IsNullOrWhiteSpace(_newCertName);
        if (!canAdd) { ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.3f)); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 0.3f)); }
        ImGui.PushStyleColor(ImGuiCol.Text, canAdd ? ColSuccess : ColMuted);
        if (ImGui.Button("✓ Confirmer##cert_confirm") && canAdd)
        {
            var certName = _newCertName;
            var origTrait = string.IsNullOrEmpty(_newCertOriginTraitId) ? null : _newCertOriginTraitId;
            var certAbId = string.IsNullOrEmpty(_newCertAbilityId) ? null : _newCertAbilityId;
            var certFp = _newCertFreePoints;
            _newCertName = _newCertOriginTraitId = _newCertAbilityId = string.Empty;
            _newCertFreePoints = 0;
            _ = Task.Run(() => _plugin.AddCertification.ExecuteAsync(_character, certName, origTrait, certAbId, certFp));
            ImGui.CloseCurrentPopup();
        }
        ImGui.PopStyleColor(canAdd ? 1 : 3);
        ImGui.SameLine();
        if (ImGui.Button("Annuler##cert_cancel")) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawEditAbilities(Rank rank)
    {
        ImGui.TextColored(ColMuted, "COMPÉTENCES"); ImGui.Spacing();
        ImGui.TextColored(ColMuted, "Points accordés par le MJ :"); ImGui.SameLine();
        ImGui.SetNextItemWidth(80); ImGui.InputInt("##skill_pts", ref _editSkillPoints, 1, 5);
        if (_editSkillPoints < 0) _editSkillPoints = 0;
        var remaining = Math.Max(0, _editSkillPoints - _character.SpentSkillPoints);
        ImGui.TextColored(ColMuted, $"Dépensés : {_character.SpentSkillPoints}  ·  Restants : {remaining}");
        if (remaining == 0 && _editSkillPoints == 0)
        {
            ImGui.SameLine(); ImGui.TextColored(ColWarn, "  ⚠ Aucun point — augmentez la valeur ci-dessus puis sauvegardez.");
        }
        ImGui.Spacing();

        if (_character.EquippedAbilities.Count > 0)
        {
            ImGui.TextColored(ColMuted, "Apprises :"); ImGui.Spacing();
            foreach (var eq in _character.EquippedAbilities.ToList())
            {
                var ab = _plugin.AbilityRepository.GetById(eq.AbilityId); if (ab is null) continue;
                var fp = _character.GetFreePointsForAbility(eq.AbilityId);
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.64f, 0.17f, 0.17f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.64f, 0.17f, 0.17f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                if (ImGui.Button($"✕##ab_rm_{eq.AbilityId}"))
                    _ = Task.Run(() => _plugin.UnequipAbility.ExecuteAsync(_character, eq.AbilityId));
                ImGui.PopStyleColor(3); ImGui.SameLine();
                ImGui.Text(ab.Name); ImGui.SameLine(); ImGui.TextColored(ColInfo, $"Lv{eq.Level}");
                if (fp > 0) { ImGui.SameLine(); ImGui.TextColored(ColSuccess, $"({fp} pt(s) certif.)"); }
                if (eq.Level < ab.MaxLevel)
                {
                    ImGui.SameLine(); var nl = eq.Level + 1; var ok = rank.AllowsAbilityLevel(nl) && remaining > 0;
                    ImGui.PushStyleColor(ImGuiCol.Button, ok ? new Vector4(0.09f, 0.37f, 0.65f, 0.20f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ok ? new Vector4(0.09f, 0.37f, 0.65f, 0.40f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                    ImGui.PushStyleColor(ImGuiCol.Text, ok ? ColInfo : ColMuted);
                    if (ImGui.Button($"↑ Lv{nl}##ab_up_{eq.AbilityId}") && ok)
                        _ = Task.Run(() => _plugin.EquipAbility.ExecuteAsync(_character, eq.AbilityId, nl));
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered() && !ok) ImGui.SetTooltip(!rank.AllowsAbilityLevel(nl) ? "Rang insuffisant." : "Pas assez de points.");
                }
                var ld = ab.Levels.FirstOrDefault(l => l.Level == eq.Level);
                if (ld != null) { ImGui.PushStyleColor(ImGuiCol.Text, ColMuted); ImGui.TextWrapped(ld.Description); ImGui.PopStyleColor(); }
                ImGui.Spacing();
            }
        }

        ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(ColMuted, "Apprendre :"); ImGui.Spacing();
        var abCats = new[] { AbilityCategory.Weapon, AbilityCategory.RoleDps, AbilityCategory.RoleSoigneur, AbilityCategory.RoleTank, AbilityCategory.Job };
        foreach (var cat in abCats)
        {
            var avail = _plugin.AbilityRepository.GetByCategory(cat)
                .Where(a => _character.GetAbilityLevel(a.Id) == 0)
                .Where(a => cat == AbilityCategory.Weapon ||
                            a.RequiredJobIds == null || a.RequiredJobIds.Count == 0 ||
                            (_character.JobId != null && a.RequiredJobIds.Contains(_character.JobId)))
                .ToList();
            if (avail.Count == 0) continue;
            ImGui.TextColored(ColMuted, AbilityCategoryLabel(cat)); ImGui.Spacing();
            foreach (var ab in avail)
            {
                var sl = ab.StartLevel; var fp = _character.GetFreePointsForAbility(ab.Id);
                var nc = Math.Max(0, sl - fp); var ok = rank.AllowsAbilityLevel(sl) && (nc == 0 || remaining > 0);
                ImGui.PushStyleColor(ImGuiCol.Button, ok ? new Vector4(0.09f, 0.37f, 0.65f, 0.20f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ok ? new Vector4(0.09f, 0.37f, 0.65f, 0.40f) : new Vector4(0.3f, 0.3f, 0.3f, 0.20f));
                ImGui.PushStyleColor(ImGuiCol.Text, ok ? ColInfo : ColMuted);
                var lbl = fp > 0 ? $"+ {ab.Name} (Lv{sl} · {fp} pt(s) certif.)##ab_learn_{ab.Id}" : $"+ {ab.Name} (Lv{sl})##ab_learn_{ab.Id}";
                if (ImGui.Button(lbl) && ok)
                    _ = Task.Run(() => _plugin.EquipAbility.ExecuteAsync(_character, ab.Id, sl));
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                {
                    var desc = ab.Levels.FirstOrDefault(l => l.Level == sl)?.Description ?? "";
                    if (!rank.AllowsAbilityLevel(sl)) desc += "\n⚠ Rang insuffisant.";
                    else if (nc > 0 && remaining <= 0) desc += "\n⚠ Pas assez de points.";
                    if (ab.UsageLimit != UsageLimit.None) desc += $"\n{UsageLimitLabel(ab.UsageLimit)}";
                    ImGui.SetTooltip(desc);
                }
            }
            ImGui.Spacing();
        }
    }

    // ------------------------------------------------------------------ Helpers

    private static string CategoryLabel(TraitCategory c) => c switch
    {
        TraitCategory.Connaissance => "Connaissances",
        TraitCategory.RoleDps => "Rôle — DPS",
        TraitCategory.RoleSoigneur => "Rôle — Soigneur",
        TraitCategory.RoleTank => "Rôle — Tank",
        TraitCategory.Job => "Job",
        _ => c.ToString(),
    };

    private static string AbilityCategoryLabel(AbilityCategory c) => c switch
    {
        AbilityCategory.Weapon => "Armes",
        AbilityCategory.RoleDps => "Rôle — DPS",
        AbilityCategory.RoleSoigneur => "Rôle — Soigneur",
        AbilityCategory.RoleTank => "Rôle — Tank",
        AbilityCategory.Job => "Job",
        _ => c.ToString(),
    };

    private static string UsageLimitLabel(UsageLimit l) => l switch
    {
        UsageLimit.OncePerCombat => "⏱ 1× par combat",
        UsageLimit.TwicePerCombat => "⏱ 2× par combat",
        UsageLimit.OncePerEvent => "⏱ 1× par event",
        UsageLimit.TwicePerEvent => "⏱ 2× par event",
        UsageLimit.ThreeTimesPerEvent => "⏱ 3× par event",
        _ => "",
    };


}

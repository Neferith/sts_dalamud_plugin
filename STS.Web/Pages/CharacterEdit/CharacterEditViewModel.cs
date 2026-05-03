using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.Repository;
using STS.Web.Services;

namespace STS.Web.ViewModels;

/// <summary>
/// ViewModel de la page d'édition d'une fiche personnage.
/// Travaille sur une copie locale du personnage et persiste via PUT après chaque mutation.
/// </summary>
public sealed class CharacterEditViewModel(
    CharacterApiService api,
    AuthService auth,
    TraitRepository traits,
    JobRepository jobs,
    AbilityRepository abilities)
{
    // ── État ──────────────────────────────────────────────────────────────────

    public Character?  Character { get; private set; }
    public bool        IsLoading { get; private set; }
    public bool        IsSaving  { get; private set; }
    public string?     Error     { get; private set; }
    public string?     Success   { get; private set; }

    public bool IsOwner =>
        auth.IsAuthenticated &&
        Character is not null &&
        Character.UserId == auth.UserId;

    public Action? OnStateChanged { get; set; }

    // ── Champs du formulaire principal ────────────────────────────────────────

    public string  FormName        { get; set; } = string.Empty;
    public string  FormHistoire    { get; set; } = string.Empty;
    public int     FormSkillPoints { get; set; }

    // ── Formulaire certification ──────────────────────────────────────────────

    public bool   ShowCertModal      { get; private set; }
    public string NewCertName        { get; set; } = string.Empty;
    public string NewCertOriginTrait { get; set; } = string.Empty;
    public string NewCertAbilityId   { get; set; } = string.Empty;
    public int    NewCertFreePoints  { get; set; }

    // ── Formulaire inventaire ─────────────────────────────────────────────────

    public bool         ShowItemModal    { get; private set; }
    public string       NewItemName      { get; set; } = string.Empty;
    public string       NewItemDesc      { get; set; } = string.Empty;
    public ItemCategory NewItemCategory  { get; set; } = ItemCategory.Item;
    public string       NewItemAbilityId { get; set; } = string.Empty;

    // ── Données de référence ──────────────────────────────────────────────────

    public IReadOnlyList<Job>   AllJobs    => jobs.GetAll();
    public IReadOnlyList<Trait> AllTraits  => traits.GetAll();

    public IReadOnlyList<Trait> OriginTraits
        => traits.GetByCategory(TraitCategory.Origine);

    public IReadOnlyList<Trait> AvailableTraits(TraitCategory category)
    {
        if (Character is null) return [];
        return [.. traits.GetByCategory(category)
            .Where(t => !Character.HasTrait(t.Id))
            .Where(t => t.RequiredJobIds == null || t.RequiredJobIds.Count == 0 ||
                        (Character.JobId != null && t.RequiredJobIds.Contains(Character.JobId)))];
    }

    public IReadOnlyList<Ability> AvailableAbilities(AbilityCategory category)
    {
        if (Character is null) return [];
        return [.. abilities.GetByCategory(category)
            .Where(a => Character.GetAbilityLevel(a.Id) == 0)
            .Where(a => category == AbilityCategory.Weapon ||
                        a.RequiredJobIds == null || a.RequiredJobIds.Count == 0 ||
                        (Character.JobId != null && a.RequiredJobIds.Contains(Character.JobId)))];
    }

    public IReadOnlyList<Ability> WeaponAbilities => abilities.GetWeapons();

    public string JobName(string? jobId)
        => jobId is null ? "— Aucun —" : jobs.GetById(jobId)?.Name ?? jobId;

    public Trait?   GetTrait(string id)   => traits.GetById(id);
    public Ability? GetAbility(string id) => abilities.GetById(id);

    public string AbilityName(string id)  => abilities.GetById(id)?.Name ?? id;
    public string TraitName(string id)    => traits.GetById(id)?.Name ?? id;

    public int RemainingSkillPoints =>
        Character is null ? 0 : Math.Max(0, FormSkillPoints - Character.SpentSkillPoints);

    public bool HasTraitConflict(Trait t)
    {
        if (Character is null || t.ExclusiveGroup is null) return false;
        return Character.EquippedTraitIds
            .Select(id => traits.GetById(id))
            .Any(x => x?.ExclusiveGroup == t.ExclusiveGroup);
    }

    public string UsageLimitLabel(UsageLimit limit) => limit switch
    {
        UsageLimit.OncePerCombat      => "⏱ 1× par combat",
        UsageLimit.TwicePerCombat     => "⏱ 2× par combat",
        UsageLimit.OncePerEvent       => "⏱ 1× par event",
        UsageLimit.TwicePerEvent      => "⏱ 2× par event",
        UsageLimit.ThreeTimesPerEvent => "⏱ 3× par event",
        _                             => string.Empty,
    };

    public static string CategoryLabel(TraitCategory c) => c switch
    {
        TraitCategory.Connaissance => "Connaissances",
        TraitCategory.RoleDps      => "Rôle — DPS",
        TraitCategory.RoleSoigneur => "Rôle — Soigneur",
        TraitCategory.RoleTank     => "Rôle — Tank",
        TraitCategory.Job          => "Job",
        _                          => c.ToString(),
    };

    public static string AbilityCategoryLabel(AbilityCategory c) => c switch
    {
        AbilityCategory.Weapon       => "Armes",
        AbilityCategory.RoleDps      => "Rôle — DPS",
        AbilityCategory.RoleSoigneur => "Rôle — Soigneur",
        AbilityCategory.RoleTank     => "Rôle — Tank",
        AbilityCategory.Job          => "Job",
        _                            => c.ToString(),
    };

    // ── Chargement ────────────────────────────────────────────────────────────

    public async Task LoadAsync(Guid id)
    {
        IsLoading = true; Notify();
        try
        {
            Character = await api.GetByIdAsync(id);
            if (Character is null) { Error = "Personnage introuvable."; return; }
            FormName        = Character.Name;
            FormHistoire    = Character.Histoire;
            FormSkillPoints = Character.SkillPoints;
        }
        catch (Exception ex) { Error = $"Erreur : {ex.Message}"; }
        finally { IsLoading = false; Notify(); }
    }

    // ── Sauvegarde principale (nom, histoire, points) ─────────────────────────

    public async Task SaveBasicInfoAsync()
    {
        if (Character is null) return;
        if (string.IsNullOrWhiteSpace(FormName)) { SetError("Le nom est requis."); return; }

        Character.Name        = FormName.Trim();
        Character.Histoire    = FormHistoire;
        Character.SkillPoints = Math.Max(0, FormSkillPoints);

        await PersistAsync("Informations sauvegardées.");
    }

    // ── Rang ──────────────────────────────────────────────────────────────────

    public async Task SetRankAsync(RankKey rank)
    {
        if (Character is null) return;
        Character.RankKey = rank;
        await PersistAsync();
    }

    // ── Race ──────────────────────────────────────────────────────────────────

    public async Task SetRaceAsync(CharacterRace race)
    {
        if (Character is null) return;
        Character.Race = race;
        await PersistAsync();
    }

    // ── Réputation ────────────────────────────────────────────────────────────

    public async Task ChangeReputationAsync(int delta)
    {
        if (Character is null) return;
        Character.ReputationLevel = Reputation.Clamp(Character.ReputationLevel + delta);
        await PersistAsync();
    }

    // ── Job ───────────────────────────────────────────────────────────────────

    public async Task SetJobAsync(string? jobId)
    {
        if (Character is null) return;
        Character.JobId = jobId;
        await PersistAsync();
    }

    // ── Trait d'origine ───────────────────────────────────────────────────────

    public async Task SetOriginTraitAsync(string? traitId)
    {
        if (Character is null) return;
        Character.OriginTraitId = traitId;
        await PersistAsync();
    }

    // ── Traits équipés ────────────────────────────────────────────────────────

    public async Task EquipTraitAsync(string traitId)
    {
        if (Character is null) return;
        if (Character.FreeTraitSlots <= 0) return;
        if (Character.HasTrait(traitId)) return;

        Character.EquippedTraitIds.Add(traitId);
        await PersistAsync();
    }

    public async Task UnequipTraitAsync(string traitId)
    {
        if (Character is null) return;
        Character.EquippedTraitIds.Remove(traitId);
        await PersistAsync();
    }

    // ── Compétences ───────────────────────────────────────────────────────────

    public async Task LearnAbilityAsync(string abilityId)
    {
        if (Character is null) return;
        var ability = abilities.GetById(abilityId);
        if (ability is null) return;

        var rank       = Rank.Get(Character.RankKey);
        var sl         = ability.StartLevel;
        var fp         = Character.GetFreePointsForAbility(abilityId);
        var cost       = Math.Max(0, sl - fp);

        if (!rank.AllowsAbilityLevel(sl)) return;
        if (cost > 0 && cost > RemainingSkillPoints) return;

        Character.EquippedAbilities.Add(new EquippedAbility { AbilityId = abilityId, Level = sl });
        await PersistAsync();
    }

    public async Task LevelUpAbilityAsync(string abilityId)
    {
        if (Character is null) return;
        var ability  = abilities.GetById(abilityId);
        var equipped = Character.EquippedAbilities.FirstOrDefault(a => a.AbilityId == abilityId);
        if (ability is null || equipped is null) return;

        var nl   = equipped.Level + 1;
        var rank = Rank.Get(Character.RankKey);
        if (!rank.AllowsAbilityLevel(nl)) return;
        if (RemainingSkillPoints <= 0) return;

        equipped.Level = nl;
        await PersistAsync();
    }

    public async Task UnequipAbilityAsync(string abilityId)
    {
        if (Character is null) return;
        Character.EquippedAbilities.RemoveAll(a => a.AbilityId == abilityId);
        await PersistAsync();
    }

    // ── Certifications ────────────────────────────────────────────────────────

    public void OpenCertModal()
    {
        NewCertName = NewCertOriginTrait = NewCertAbilityId = string.Empty;
        NewCertFreePoints = 0;
        ShowCertModal = true;
        Notify();
    }

    public void CloseCertModal() { ShowCertModal = false; Notify(); }

    public async Task AddCertificationAsync()
    {
        if (Character is null) return;
        if (string.IsNullOrWhiteSpace(NewCertName)) { SetError("Le nom est requis."); return; }

        Character.Certifications.Add(new Certification
        {
            Name                = NewCertName.Trim(),
            LinkedOriginTraitId = string.IsNullOrEmpty(NewCertOriginTrait) ? null : NewCertOriginTrait,
            LinkedAbilityId     = string.IsNullOrEmpty(NewCertAbilityId)   ? null : NewCertAbilityId,
            FreePoints          = Math.Max(0, NewCertFreePoints),
        });

        ShowCertModal = false;
        await PersistAsync("Certification ajoutée.");
    }

    public async Task RemoveCertificationAsync(string certId)
    {
        if (Character is null) return;
        Character.Certifications.RemoveAll(c => c.Id == certId);
        await PersistAsync();
    }

    // ── Inventaire ────────────────────────────────────────────────────────────

    public void OpenItemModal()
    {
        NewItemName = NewItemDesc = NewItemAbilityId = string.Empty;
        NewItemCategory = ItemCategory.Item;
        ShowItemModal = true;
        Notify();
    }

    public void CloseItemModal() { ShowItemModal = false; Notify(); }

    public async Task AddItemAsync()
    {
        if (Character is null) return;
        if (string.IsNullOrWhiteSpace(NewItemName)) { SetError("Le nom est requis."); return; }

        Character.Inventory.Add(new CharacterItem
        {
            Name            = NewItemName.Trim(),
            Description     = NewItemDesc.Trim(),
            Category        = NewItemCategory,
            LinkedAbilityId = string.IsNullOrEmpty(NewItemAbilityId) ? null : NewItemAbilityId,
            SortIndex       = Character.Inventory.Count,
        });

        ShowItemModal = false;
        await PersistAsync("Objet ajouté.");
    }

    public async Task RemoveItemAsync(string itemId)
    {
        if (Character is null) return;
        if (Character.MainHandItemId == itemId) Character.MainHandItemId = null;
        if (Character.OffHandItemId  == itemId) Character.OffHandItemId  = null;
        Character.Inventory.RemoveAll(i => i.Id == itemId);
        // Réindexer
        for (var i = 0; i < Character.Inventory.Count; i++)
            Character.Inventory[i].SortIndex = i;
        await PersistAsync();
    }

    public async Task SetItemSlotAsync(string slot, string? itemId)
    {
        if (Character is null) return;
        if (slot == "main") Character.MainHandItemId = itemId;
        else                Character.OffHandItemId  = itemId;

        foreach (var item in Character.Inventory)
            item.IsEquipped = item.Id == Character.MainHandItemId || item.Id == Character.OffHandItemId;

        await PersistAsync();
    }

    // ── Persistance ───────────────────────────────────────────────────────────

    private async Task PersistAsync(string? successMsg = null)
    {
        if (Character is null) return;
        IsSaving = true; ClearMessages(); Notify();
        try
        {
            var error = await api.UpdateAsync(Character);
            if (error is not null) SetError(error);
            else if (successMsg is not null) SetSuccess(successMsg);
        }
        finally { IsSaving = false; Notify(); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetError(string msg)   { Error = msg;   Success = null; Notify(); }
    private void SetSuccess(string msg) { Success = msg; Error = null;   Notify(); }
    private void ClearMessages()        { Error = null;  Success = null; }
    private void Notify() => OnStateChanged?.Invoke();
}

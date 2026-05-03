using Sts.Admin.Services;
using Sts.Admin.Models;
using Sts.Domain;
using Sts.Domain.Character;

namespace Sts.Admin.ViewModels;

/// <summary>ViewModel de la page de gestion des fiches personnages.</summary>
public sealed class CharactersViewModel(ApiClient api)
{
    // ── Liste ─────────────────────────────────────────────────────────────────

    public List<Character> Characters { get; private set; } = [];
    public Dictionary<Guid, string> UserNames { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }
    public string? Success { get; private set; }
    public Action? OnStateChanged { get; set; }

    // ── Filtres & tri ─────────────────────────────────────────────────────────

    public string Search { get; set; } = string.Empty;
    public string FilterRank { get; set; } = string.Empty;
    public string SortBy { get; private set; } = "name";
    public bool SortAsc { get; private set; } = true;

    public IReadOnlyList<Character> Filtered
    {
        get
        {
            var q = Characters.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.Trim().ToLowerInvariant();
                q = q.Where(c =>
                    c.Name.ToLowerInvariant().Contains(s) ||
                    (c.UserId.HasValue && UserNames.TryGetValue(c.UserId.Value, out var u) &&
                     u.ToLowerInvariant().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(FilterRank) &&
                Enum.TryParse<RankKey>(FilterRank, out var rank))
                q = q.Where(c => c.RankKey == rank);

            q = SortBy switch
            {
                "rank" => SortAsc ? q.OrderBy(c => c.RankKey) : q.OrderByDescending(c => c.RankKey),
                "player" => SortAsc ? q.OrderBy(c => PlayerName(c)) : q.OrderByDescending(c => PlayerName(c)),
                _ => SortAsc ? q.OrderBy(c => c.Name) : q.OrderByDescending(c => c.Name),
            };

            return q.ToList();
        }
    }

    public string PlayerName(Character c)
        => c.UserId.HasValue && UserNames.TryGetValue(c.UserId.Value, out var u) ? u : "—";

    public void SetSort(string col)
    {
        if (SortBy == col) SortAsc = !SortAsc;
        else { SortBy = col; SortAsc = true; }
        Notify();
    }

    public string SortIcon(string col)
        => SortBy != col ? " ⇅" : SortAsc ? " ↑" : " ↓";

    public void ResetFilters() { Search = ""; FilterRank = ""; Notify(); }

    // ── Modale modération ─────────────────────────────────────────────────────

    public Character? EditTarget { get; private set; }
    public bool ShowEditModal { get; private set; }

    // Rang
    public RankKey FormRank { get; set; }

    // Points de compétence
    public int FormSkillPoints { get; set; }

    // Certifications
    public List<Certification> FormCertifications { get; private set; } = [];
    public string FormCertName { get; set; } = string.Empty;
    public string FormCertLinkedAbilityId { get; set; } = string.Empty;
    public string FormCertLinkedOriginTraitId { get; set; } = string.Empty;
    public int FormCertFreePoints { get; set; }

    public bool IsSaving { get; private set; }

    public void OpenEdit(Character c)
    {
        EditTarget = c;
        FormRank = c.RankKey;
        FormSkillPoints = c.SkillPoints;
        FormCertifications = c.Certifications.Select(x => new Certification
        {
            Name = x.Name,
            LinkedAbilityId = x.LinkedAbilityId,
            LinkedOriginTraitId = x.LinkedOriginTraitId,
            FreePoints = x.FreePoints,
        }).ToList();
        ResetCertForm();
        ShowEditModal = true;
        ClearMessages();
        Notify();
    }

    public void CloseEdit() { ShowEditModal = false; EditTarget = null; Notify(); }

    public void AddCertification()
    {
        if (string.IsNullOrWhiteSpace(FormCertName)) return;
        FormCertifications.Add(new Certification
        {
            Name = FormCertName.Trim(),
            LinkedAbilityId = string.IsNullOrWhiteSpace(FormCertLinkedAbilityId) ? null : FormCertLinkedAbilityId.Trim(),
            LinkedOriginTraitId = string.IsNullOrWhiteSpace(FormCertLinkedOriginTraitId) ? null : FormCertLinkedOriginTraitId.Trim(),
            FreePoints = FormCertFreePoints,
        });
        ResetCertForm();
        Notify();
    }

    public void RemoveCertification(Certification cert)
    {
        FormCertifications.Remove(cert);
        Notify();
    }

    public async Task SaveEditAsync()
    {
        if (EditTarget is null) return;

        IsSaving = true; Notify();
        try
        {
            // Appliquer les modifications sur une copie
            var updated = EditTarget;
            updated.RankKey = FormRank;
            updated.SkillPoints = FormSkillPoints;
            updated.Certifications = FormCertifications;

            var (_, error) = await api.PutAsync<object>(
                $"/api/characters/{updated.Id}", updated);

            if (error is not null) SetError(error);
            else
            {
                SetSuccess($"Fiche « {updated.Name} » mise à jour.");
                CloseEdit();
                await LoadAsync();
            }
        }
        finally { IsSaving = false; Notify(); }
    }

    // ── Suppression ───────────────────────────────────────────────────────────

    public async Task DeleteAsync(Character c)
    {
        ClearMessages();
        var error = await api.DeleteAsync($"/api/characters/{c.Id}");
        if (error is not null) SetError(error);
        else
        {
            SetSuccess($"Fiche « {c.Name} » supprimée.");
            await LoadAsync();
        }
    }

    // ── Chargement ────────────────────────────────────────────────────────────

    public async Task LoadAsync()
    {
        IsLoading = true; Notify();
        try
        {
            var characters = await api.GetAsync<List<Character>>("/api/characters") ?? [];
            var users = await api.GetAsync<List<UserDto>>("/api/users") ?? [];

            Characters = characters;
            UserNames = users.ToDictionary(u => u.Id, u => u.Username);
        }
        finally { IsLoading = false; Notify(); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ResetCertForm()
    {
        FormCertName = string.Empty;
        FormCertLinkedAbilityId = string.Empty;
        FormCertLinkedOriginTraitId = string.Empty;
        FormCertFreePoints = 0;
    }

    private void SetError(string msg) { Error = msg; Success = null; Notify(); }
    private void SetSuccess(string msg) { Success = msg; Error = null; Notify(); }
    private void ClearMessages() { Error = null; Success = null; }
    private void Notify() => OnStateChanged?.Invoke();
}

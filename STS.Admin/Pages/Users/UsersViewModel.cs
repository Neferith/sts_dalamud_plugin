using Sts.Admin.Models;
using Sts.Admin.Services;
using Sts.Domain.User;

namespace Sts.Admin.ViewModels;

/// <summary>ViewModel de la page de gestion des utilisateurs.</summary>
public sealed class UsersViewModel(ApiClient api)
{
    // ── État de la liste ──────────────────────────────────────────────────────

    public List<UserDto> Users { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }
    public string? Success { get; private set; }

    // ── Filtres & tri ─────────────────────────────────────────────────────────

    public string Search { get; set; } = string.Empty;
    public string FilterRole { get; set; } = string.Empty;
    public string SortBy { get; private set; } = "username";
    public bool SortAsc { get; private set; } = true;

    public IReadOnlyList<UserDto> Filtered
    {
        get
        {
            var q = Users.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.Trim().ToLowerInvariant();
                q = q.Where(u => u.Username.ToLowerInvariant().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(FilterRole) &&
                Enum.TryParse<UserRole>(FilterRole, out var role))
            {
                q = q.Where(u => u.Role == role);
            }

            q = SortBy switch
            {
                "role" => SortAsc ? q.OrderBy(u => u.Role).ThenBy(u => u.Username)
                                       : q.OrderByDescending(u => u.Role).ThenBy(u => u.Username),
                "createdAt" => SortAsc ? q.OrderBy(u => u.CreatedAt)
                                       : q.OrderByDescending(u => u.CreatedAt),
                _ => SortAsc ? q.OrderBy(u => u.Username)
                                       : q.OrderByDescending(u => u.Username),
            };

            return q.ToList();
        }
    }

    public void SetSort(string col)
    {
        if (SortBy == col) SortAsc = !SortAsc;
        else { SortBy = col; SortAsc = true; }
        Notify();
    }

    public string SortIcon(string col)
        => SortBy != col ? " ⇅" : SortAsc ? " ↑" : " ↓";

    public void ResetFilters() { Search = ""; FilterRole = ""; Notify(); }

    // ── Formulaire création ───────────────────────────────────────────────────

    public bool ShowCreateModal { get; private set; }
    public string FormUsername { get; set; } = string.Empty;
    public string FormPassword { get; set; } = string.Empty;
    public UserRole FormRole { get; set; } = UserRole.Member;
    public bool IsSaving { get; private set; }

    public void OpenCreate()
    {
        FormUsername = string.Empty;
        FormPassword = string.Empty;
        FormRole = UserRole.Member;
        ShowCreateModal = true;
        ClearMessages();
        Notify();
    }

    public void CloseCreate() { ShowCreateModal = false; Notify(); }

    public async Task SaveCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(FormUsername)) { SetError("Le nom d'utilisateur est requis."); return; }
        if (string.IsNullOrWhiteSpace(FormPassword)) { SetError("Le mot de passe est requis."); return; }

        IsSaving = true; Notify();
        try
        {
            var (_, error) = await api.PostAsync<UserDto>("/api/users", new
            {
                username = FormUsername.Trim(),
                password = FormPassword,
                role = (int)FormRole,
            });

            if (error is not null) SetError(error);
            else
            {
                SetSuccess($"Utilisateur « {FormUsername.Trim()} » créé.");
                CloseCreate();
                await LoadAsync();
            }
        }
        finally { IsSaving = false; Notify(); }
    }

    // ── Formulaire reset mot de passe ─────────────────────────────────────────

    public bool ShowResetModal { get; private set; }
    public UserDto? ResetTarget { get; private set; }
    public string FormNewPassword { get; set; } = string.Empty;
    public bool IsResetting { get; private set; }

    public void OpenReset(UserDto user)
    {
        ResetTarget = user;
        FormNewPassword = string.Empty;
        ShowResetModal = true;
        ClearMessages();
        Notify();
    }

    public void CloseReset() { ShowResetModal = false; ResetTarget = null; Notify(); }

    public async Task SaveResetAsync()
    {
        if (ResetTarget is null) return;
        if (string.IsNullOrWhiteSpace(FormNewPassword)) { SetError("Le nouveau mot de passe est requis."); return; }

        IsResetting = true; Notify();
        try
        {
            var (_, error) = await api.PutAsync<object>(
                $"/api/users/{ResetTarget.Id}/password",
                new { newPassword = FormNewPassword });

            if (error is not null) SetError(error);
            else
            {
                SetSuccess($"Mot de passe de « {ResetTarget.Username} » réinitialisé.");
                CloseReset();
            }
        }
        finally { IsResetting = false; Notify(); }
    }

    // ── Suppression ───────────────────────────────────────────────────────────

    public async Task DeleteAsync(UserDto user)
    {
        ClearMessages();
        var error = await api.DeleteAsync($"/api/users/{user.Id}");
        if (error is not null) SetError(error);
        else
        {
            SetSuccess($"Utilisateur « {user.Username} » supprimé.");
            await LoadAsync();
        }
    }

    // ── Chargement ────────────────────────────────────────────────────────────

    public async Task LoadAsync()
    {
        IsLoading = true; Notify();
        Users = await api.GetAsync<List<UserDto>>("/api/users") ?? [];
        IsLoading = false; Notify();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public Action? OnStateChanged { get; set; }

    private void SetError(string msg) { Error = msg; Success = null; Notify(); }
    private void SetSuccess(string msg) { Success = msg; Error = null; Notify(); }
    private void ClearMessages() { Error = null; Success = null; }
    private void Notify() => OnStateChanged?.Invoke();
}


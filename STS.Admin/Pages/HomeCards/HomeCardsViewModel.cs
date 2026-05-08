using Sts.Domain.Content.Models;
using Sts.Domain.Content.UseCases;

namespace STS.Admin.ViewModels;

/// <summary>ViewModel de la page de gestion des cartes home.</summary>
public sealed class HomeCardsViewModel(
    IGetHomeCardsUseCase get,
    ICreateHomeCardUseCase create,
    IUpdateHomeCardUseCase update,
    IDeleteHomeCardUseCase delete)
{
    // ── État ──────────────────────────────────────────────────────────────────

    public IReadOnlyList<HomeCard> Cards { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public bool IsSaving { get; private set; }
    public bool ShowForm { get; private set; }
    public Guid? EditingId { get; private set; }
    public string? Error { get; private set; }
    public string? Success { get; private set; }

    public bool IsEditing => EditingId.HasValue;

    // ── Champs du formulaire ──────────────────────────────────────────────────

    public string FormTitle { get; set; } = string.Empty;
    public string FormDescription { get; set; } = string.Empty;
    public string FormIcon { get; set; } = string.Empty;
    public string FormLinkUrl { get; set; } = string.Empty;
    public string FormLinkLabel { get; set; } = string.Empty;
    public string FormAccent { get; set; } = "teal";
    public int FormOrder { get; set; }

    public bool FormIsFeatured { get; set; }
    public bool FormIsVisible { get; set; } = true;

    public Action? OnStateChanged { get; set; }

    // ── Commandes ─────────────────────────────────────────────────────────────

    public async Task LoadAsync()
    {
        IsLoading = true; Error = null; Notify();
        try { Cards = await get.ExecuteAsync(); }
        catch (Exception ex) { Error = $"Erreur : {ex.Message}"; }
        finally { IsLoading = false; Notify(); }
    }

    public void StartCreate()
    {
        EditingId = null;
        FormTitle = string.Empty;
        FormDescription = string.Empty;
        FormIcon = string.Empty;
        FormLinkUrl = string.Empty;
        FormLinkLabel = string.Empty;
        FormAccent = "teal";
        FormOrder = Cards.Count > 0 ? Cards.Max(c => c.Order) + 1 : 0;
        FormIsVisible = true;
        ShowForm = true;
        Success = null;
        Notify();
    }

    public void StartEdit(HomeCard card)
    {
        EditingId = card.Id;
        FormTitle = card.Title;
        FormDescription = card.Description;
        FormIcon = card.Icon ?? string.Empty;
        FormLinkUrl = card.LinkUrl ?? string.Empty;
        FormLinkLabel = card.LinkLabel ?? string.Empty;
        FormAccent = card.Accent;
        FormOrder = card.Order;
        FormIsFeatured = card.IsFeatured;
        FormIsVisible = card.IsVisible;
        ShowForm = true;
        Success = null;
        Notify();
    }

    public void Cancel()
    {
        ShowForm = false; EditingId = null; Notify();
    }

    public async Task SaveAsync()
    {
        IsSaving = true; Error = null; Success = null; Notify();
        try
        {
            var card = new HomeCard
            {
                Id = EditingId ?? Guid.NewGuid(),
                Title = FormTitle,
                Description = FormDescription,
                Icon = string.IsNullOrWhiteSpace(FormIcon) ? null : FormIcon,
                LinkUrl = string.IsNullOrWhiteSpace(FormLinkUrl) ? null : FormLinkUrl,
                LinkLabel = string.IsNullOrWhiteSpace(FormLinkLabel) ? null : FormLinkLabel,
                Accent = FormAccent,
                Order = FormOrder,
                IsFeatured = FormIsFeatured,
                IsVisible = FormIsVisible,
            };

            if (IsEditing)
                await update.ExecuteAsync(card);
            else
                await create.ExecuteAsync(card);

            Success = IsEditing ? "Carte mise à jour." : "Carte créée.";
            ShowForm = false;
            EditingId = null;
            await LoadAsync();
        }
        catch (Exception ex) { Error = $"Erreur : {ex.Message}"; }
        finally { IsSaving = false; Notify(); }
    }

    public async Task DeleteAsync(Guid id)
    {
        Error = null; Success = null; Notify();
        try
        {
            await delete.ExecuteAsync(id);
            Success = "Carte supprimée.";
            await LoadAsync();
        }
        catch (Exception ex) { Error = $"Erreur : {ex.Message}"; }
    }

    private void Notify() => OnStateChanged?.Invoke();
}

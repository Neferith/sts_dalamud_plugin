using Sts.Domain.Content;
using Sts.Domain.Content.Models;
using Sts.Domain.Content.UseCases;

namespace STS.Admin.ViewModels;

/// <summary>ViewModel de la page de gestion des liens rapides.</summary>
public sealed class QuickLinksViewModel(
    IGetQuickLinksUseCase getAll,
    ICreateQuickLinkUseCase create,
    IUpdateQuickLinkUseCase update,
    IDeleteQuickLinkUseCase delete)
{
    // ── État liste ────────────────────────────────────────────────────────────

    /// <summary>Liens rapides chargés.</summary>
    public IReadOnlyList<QuickLink> Items { get; private set; } = [];

    /// <summary>Indique si un chargement est en cours.</summary>
    public bool IsLoading { get; private set; }

    /// <summary>Message d'erreur courant, ou <see langword="null"/>.</summary>
    public string? Error { get; private set; }

    // ── État formulaire ───────────────────────────────────────────────────────

    /// <summary>Lien en cours d'édition, ou <see langword="null"/> si aucun.</summary>
    public QuickLink? Editing { get; private set; }

    /// <summary>Indique si le panneau de création/édition est ouvert.</summary>
    public bool IsPanelOpen { get; private set; }

    /// <summary>Indique si une sauvegarde est en cours.</summary>
    public bool IsSaving { get; private set; }

    // ── Champs du formulaire ─────────────────────────────────────────────────

    public string FormLabel { get; set; } = string.Empty;
    public string FormUrl { get; set; } = string.Empty;
    public string FormIcon { get; set; } = string.Empty;
    public QuickLinkCategory FormCategory { get; set; } = QuickLinkCategory.Ressources;
    public int FormOrder { get; set; }
    public bool FormIsVisible { get; set; } = true;

    // ── Notification ──────────────────────────────────────────────────────────

    /// <summary>Déclenché quand l'état change — le composant appelle <c>StateHasChanged</c>.</summary>
    public Action? OnStateChanged { get; set; }

    // ── Commandes ─────────────────────────────────────────────────────────────

    /// <summary>Charge la liste des liens depuis l'API.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        Notify();
        try
        {
            Items = (await getAll.ExecuteAsync()).ToList();
        }
        catch (Exception ex)
        {
            Error = $"Erreur lors du chargement : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            Notify();
        }
    }

    /// <summary>Ouvre le panneau de création.</summary>
    public void OpenCreate()
    {
        Editing = null;
        FormLabel = string.Empty;
        FormUrl = string.Empty;
        FormIcon = string.Empty;
        FormCategory = QuickLinkCategory.Ressources;
        FormOrder = Items.Count > 0 ? Items.Max(l => l.Order) + 1 : 0;
        FormIsVisible = true;
        IsPanelOpen = true;
        Notify();
    }

    /// <summary>Ouvre le panneau d'édition pour un lien existant.</summary>
    public void OpenEdit(QuickLink link)
    {
        Editing = link;
        FormLabel = link.Label;
        FormUrl = link.Url;
        FormIcon = link.Icon ?? string.Empty;
        FormCategory = link.Category;
        FormOrder = link.Order;
        FormIsVisible = link.IsVisible;
        IsPanelOpen = true;
        Notify();
    }

    /// <summary>Ferme le panneau sans sauvegarder.</summary>
    public void ClosePanel()
    {
        IsPanelOpen = false;
        Editing = null;
        Notify();
    }

    /// <summary>Sauvegarde le formulaire (création ou mise à jour).</summary>
    public async Task SaveAsync()
    {
        IsSaving = true;
        Error = null;
        Notify();
        try
        {
            if (Editing is null)
            {
                var parameters = new CreateQuickLinkParameters(
                    FormLabel, FormUrl,
                    string.IsNullOrWhiteSpace(FormIcon) ? null : FormIcon,
                    FormCategory, FormOrder, FormIsVisible);
                await create.ExecuteAsync(parameters);
            }
            else
            {
                var parameters = new UpdateQuickLinkParameters(
                    FormLabel, FormUrl,
                    string.IsNullOrWhiteSpace(FormIcon) ? null : FormIcon,
                    FormCategory, FormOrder, FormIsVisible);
                await update.ExecuteAsync(Editing.Id, parameters);
            }
            IsPanelOpen = false;
            Editing = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Error = $"Erreur lors de la sauvegarde : {ex.Message}";
        }
        finally
        {
            IsSaving = false;
            Notify();
        }
    }

    /// <summary>Supprime un lien rapide.</summary>
    public async Task DeleteAsync(Guid id)
    {
        Error = null;
        try
        {
            await delete.ExecuteAsync(id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Error = $"Erreur lors de la suppression : {ex.Message}";
            Notify();
        }
    }

    private void Notify() => OnStateChanged?.Invoke();
}

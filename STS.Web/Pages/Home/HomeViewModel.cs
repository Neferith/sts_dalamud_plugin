using Sts.Domain.Content.Models;
using Sts.Domain.Content.UseCases;

namespace STS.Web.Pages.Home;

/// <summary>ViewModel de la page d'accueil publique.</summary>
public sealed class HomeViewModel(
    IGetVisibleQuickLinksUseCase getLinks,
    IGetSiteSettingsUseCase getSettings)
{
    // ── État ──────────────────────────────────────────────────────────────────

    /// <summary>Paramètres éditoriaux du site.</summary>
    public SiteSettings Settings { get; private set; } = new();

    /// <summary>Liens rapides de la section Recrutement.</summary>
    public IReadOnlyList<QuickLink> RecruitmentLinks { get; private set; } = [];

    /// <summary>Liens rapides de la section Ressources.</summary>
    public IReadOnlyList<QuickLink> ResourceLinks { get; private set; } = [];

    /// <summary>Indique si un chargement est en cours.</summary>
    public bool IsLoading { get; private set; }

    /// <summary>Message d'erreur courant, ou <see langword="null"/>.</summary>
    public string? Error { get; private set; }

    /// <summary>Déclenché quand l'état change.</summary>
    public Action? OnStateChanged { get; set; }

    // ── Commandes ─────────────────────────────────────────────────────────────

    /// <summary>Charge les données depuis l'API.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        Notify();
        try
        {
            var settingsTask = getSettings.ExecuteAsync();
            var linksTask = getLinks.ExecuteAsync();
            await Task.WhenAll(settingsTask, linksTask);

            Settings = settingsTask.Result;
            var all = linksTask.Result.ToList();
            RecruitmentLinks = all.Where(l => l.Category == QuickLinkCategory.Recrutement).ToList();
            ResourceLinks = all.Where(l => l.Category == QuickLinkCategory.Ressources).ToList();
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

    private void Notify() => OnStateChanged?.Invoke();
}

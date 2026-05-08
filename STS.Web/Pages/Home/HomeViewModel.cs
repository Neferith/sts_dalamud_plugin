using Sts.Domain.Content.Models;
using Sts.Domain.Content.UseCases;

namespace STS.Web.Pages.Home;

public sealed class HomeViewModel(
    IGetVisibleQuickLinksUseCase getLinks,
    IGetSiteSettingsUseCase getSettings,
    IGetVisibleHomeCardsUseCase getHomeCards)
{
    public SiteSettings Settings { get; private set; } = new();
    public IReadOnlyList<QuickLink> RecruitmentLinks { get; private set; } = [];
    public IReadOnlyList<QuickLink> ResourceLinks { get; private set; } = [];
    public IReadOnlyList<HomeCard> HomeCards { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }
    public Action? OnStateChanged { get; set; }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        Notify();
        try
        {
            var settingsTask = getSettings.ExecuteAsync();
            var linksTask = getLinks.ExecuteAsync();
            var cardsTask = getHomeCards.ExecuteAsync();
            await Task.WhenAll(settingsTask, linksTask, cardsTask);

            Settings = settingsTask.Result;
            var all = linksTask.Result.ToList();
            RecruitmentLinks = all.Where(l => l.Category == QuickLinkCategory.Recrutement).ToList();
            ResourceLinks = all.Where(l => l.Category == QuickLinkCategory.Ressources).ToList();
            HomeCards = cardsTask.Result;
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

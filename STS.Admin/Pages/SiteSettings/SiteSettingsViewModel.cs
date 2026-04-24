using Sts.Domain.Content;
using Sts.Domain.Content.UseCases;

namespace STS.Admin.ViewModels;

/// <summary>ViewModel de la page des paramètres éditoriaux.</summary>
public sealed class SiteSettingsViewModel(
    IGetSiteSettingsUseCase get,
    IUpdateSiteSettingsUseCase update)
{
    public bool IsLoading { get; private set; }
    public bool IsSaving { get; private set; }
    public string? Error { get; private set; }
    public string? Success { get; private set; }

    // ── Champs du formulaire ─────────────────────────────────────────────────

    public string FormHeroTitle { get; set; } = string.Empty;
    public string FormHeroText { get; set; } = string.Empty;
    public string FormWorld { get; set; } = string.Empty;
    public string FormDataCenter { get; set; } = string.Empty;

    public Action? OnStateChanged { get; set; }

    // ── Commandes ─────────────────────────────────────────────────────────────

    /// <summary>Charge les paramètres depuis l'API.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        Notify();
        try
        {
            var settings = await get.ExecuteAsync();
            FormHeroTitle = settings.HeroTitle;
            FormHeroText = settings.HeroText;
            FormWorld = settings.World;
            FormDataCenter = settings.DataCenter;
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

    /// <summary>Sauvegarde les paramètres.</summary>
    public async Task SaveAsync()
    {
        IsSaving = true;
        Error = null;
        Success = null;
        Notify();
        try
        {
            var settings = new SiteSettings
            {
                HeroTitle = FormHeroTitle,
                HeroText = FormHeroText,
                World = FormWorld,
                DataCenter = FormDataCenter,
            };
            await update.ExecuteAsync(settings);
            Success = "Paramètres sauvegardés.";
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

    private void Notify() => OnStateChanged?.Invoke();
}

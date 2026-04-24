using Sts.Domain.Content;
using Sts.Domain.Content.UseCases;

namespace Sts.Api.Endpoints;

/// <summary>Endpoints pour les paramètres éditoriaux du site.</summary>
public static class SiteSettingsEndpoints
{
    /// <summary>Enregistre les routes <c>/api/site-settings</c>.</summary>
    public static void MapSiteSettingsEndpoints(this WebApplication app)
    {
        // ── Public ────────────────────────────────────────────────────────────

        app.MapGet("/api/site-settings", async (IGetSiteSettingsUseCase useCase) =>
            Results.Ok(await useCase.ExecuteAsync()))
            .WithName("GetSiteSettings")
            .WithTags("SiteSettings")
            .WithSummary("Retourne les paramètres éditoriaux du site.")
            .AllowAnonymous();

        // ── Admin (JWT requis) ────────────────────────────────────────────────

        app.MapPut("/api/site-settings", async (
            SiteSettings settings,
            IUpdateSiteSettingsUseCase useCase) =>
            Results.Ok(await useCase.ExecuteAsync(settings)))
            .WithName("UpdateSiteSettings")
            .WithTags("SiteSettings")
            .WithSummary("Met à jour les paramètres éditoriaux du site.")
            .RequireAuthorization();
    }
}

using Sts.Domain.Content;
using Sts.Domain.Content.UseCases;

namespace Sts.Api.Endpoints;

/// <summary>Endpoints pour la gestion des liens rapides.</summary>
public static class QuickLinksEndpoints
{
    /// <summary>Enregistre les routes <c>/api/quick-links</c>.</summary>
    public static void MapQuickLinksEndpoints(this WebApplication app)
    {
        // ── Public ────────────────────────────────────────────────────────────

        app.MapGet("/api/quick-links", async (IGetVisibleQuickLinksUseCase useCase) =>
            Results.Ok(await useCase.ExecuteAsync()))
            .WithName("GetVisibleQuickLinks")
            .WithTags("QuickLinks")
            .WithSummary("Retourne les liens rapides visibles (home publique).")
            .AllowAnonymous();

        // ── Admin (JWT requis) ────────────────────────────────────────────────

        app.MapGet("/api/quick-links/all", async (IGetQuickLinksUseCase useCase) =>
            Results.Ok(await useCase.ExecuteAsync()))
            .WithName("GetAllQuickLinks")
            .WithTags("QuickLinks")
            .WithSummary("Retourne tous les liens rapides (admin).")
            .RequireAuthorization();

        app.MapPost("/api/quick-links", async (
            CreateQuickLinkParameters parameters,
            ICreateQuickLinkUseCase useCase) =>
        {
            var link = await useCase.ExecuteAsync(parameters);
            return Results.Created($"/api/quick-links/{link.Id}", link);
        })
        .WithName("CreateQuickLink")
        .WithTags("QuickLinks")
        .WithSummary("Crée un nouveau lien rapide.")
        .RequireAuthorization();

        app.MapPut("/api/quick-links/{id:guid}", async (
            Guid id,
            UpdateQuickLinkParameters parameters,
            IUpdateQuickLinkUseCase useCase) =>
        {
            var link = await useCase.ExecuteAsync(id, parameters);
            return link is null ? Results.NotFound() : Results.Ok(link);
        })
        .WithName("UpdateQuickLink")
        .WithTags("QuickLinks")
        .WithSummary("Met à jour un lien rapide existant.")
        .RequireAuthorization();

        app.MapDelete("/api/quick-links/{id:guid}", async (
            Guid id,
            IDeleteQuickLinkUseCase useCase) =>
        {
            var deleted = await useCase.ExecuteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteQuickLink")
        .WithTags("QuickLinks")
        .WithSummary("Supprime un lien rapide.")
        .RequireAuthorization();
    }
}

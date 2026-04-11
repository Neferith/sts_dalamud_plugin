using Microsoft.AspNetCore.Mvc;
using Sts.Api.Services;
using Sts.Domain.Content;
using Sts.Domain.Content.UseCases;

namespace Sts.Api.Endpoints;

// ── DTOs ──────────────────────────────────────────────────────────────────────

internal sealed record CreateSectionRequest(string Id, string Title, int Order);
internal sealed record UpdateSectionRequest(string Title, int Order);
internal sealed record CreatePostRequest(string Id, string Title, string Content);
internal sealed record UpdatePostRequest(string Title, string Content);

// ── Endpoints ─────────────────────────────────────────────────────────────────

/// <summary>Endpoints de gestion des sections et posts de règles.</summary>
public static class RulesEndpoints
{
    public static void MapRulesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rules").WithTags("Rules");

        // ── Lecture publique ──────────────────────────────────────────────────

        group.MapGet("/", async (IGetRulesUseCase uc) =>
            Results.Ok(await uc.ExecuteAsync()))
             .AllowAnonymous()
             .WithName("GetRules")
             .WithSummary("Retourne toutes les sections de règles triées.");

        // ── Sections ──────────────────────────────────────────────────────────

        group.MapPost("/sections", async (
            [FromBody] CreateSectionRequest req,
            ICreateSectionUseCase uc) =>
        {
            var section = new RulesSection { Id = req.Id, Title = req.Title, Order = req.Order };

            return await uc.ExecuteAsync(section)
                ? Results.Created($"/api/rules/sections/{req.Id}", section)
                : Results.Conflict($"Une section avec l'ID '{req.Id}' existe déjà.");
        })
        .RequireAuthorization()
        .WithName("CreateSection")
        .WithSummary("Crée une nouvelle section de règles.");

        group.MapPut("/sections/{sectionId}", async (
            string sectionId,
            [FromBody] UpdateSectionRequest req,
            IUpdateSectionUseCase uc) =>
        {
            return await uc.ExecuteAsync(sectionId, req.Title, req.Order)
                ? Results.NoContent()
                : Results.NotFound($"Section '{sectionId}' introuvable.");
        })
        .RequireAuthorization()
        .WithName("UpdateSection")
        .WithSummary("Met à jour le titre et l'ordre d'une section.");

        group.MapDelete("/sections/{sectionId}", async (
            string sectionId,
            IDeleteSectionUseCase uc) =>
        {
            return await uc.ExecuteAsync(sectionId)
                ? Results.NoContent()
                : Results.NotFound($"Section '{sectionId}' introuvable.");
        })
        .RequireAuthorization()
        .WithName("DeleteSection")
        .WithSummary("Supprime une section et tous ses posts.");

        // ── Posts ─────────────────────────────────────────────────────────────

        group.MapPost("/sections/{sectionId}/posts", async (
            string sectionId,
            [FromBody] CreatePostRequest req,
            ICreatePostUseCase uc) =>
        {
            var post = new RulesPost { Id = req.Id, Title = req.Title, Content = req.Content };

            return await uc.ExecuteAsync(sectionId, post) switch
            {
                true => Results.Created($"/api/rules/sections/{sectionId}/posts/{req.Id}", post),
                false => Results.Conflict($"Un post avec l'ID '{req.Id}' existe déjà dans '{sectionId}'."),
                null => Results.NotFound($"Section '{sectionId}' introuvable."),
            };
        })
        .RequireAuthorization()
        .WithName("CreatePost")
        .WithSummary("Crée un article dans une section.");

        group.MapPut("/sections/{sectionId}/posts/{postId}", async (
            string sectionId,
            string postId,
            [FromBody] UpdatePostRequest req,
            IUpdatePostUseCase uc) =>
        {
            return await uc.ExecuteAsync(sectionId, postId, req.Title, req.Content)
                ? Results.NoContent()
                : Results.NotFound($"Section '{sectionId}' ou post '{postId}' introuvable.");
        })
        .RequireAuthorization()
        .WithName("UpdatePost")
        .WithSummary("Met à jour le titre et le contenu d'un article.");

        group.MapDelete("/sections/{sectionId}/posts/{postId}", async (
            string sectionId,
            string postId,
            IDeletePostUseCase uc) =>
        {
            return await uc.ExecuteAsync(sectionId, postId)
                ? Results.NoContent()
                : Results.NotFound($"Section '{sectionId}' ou post '{postId}' introuvable.");
        })
        .RequireAuthorization()
        .WithName("DeletePost")
        .WithSummary("Supprime un article d'une section.");
    }
}

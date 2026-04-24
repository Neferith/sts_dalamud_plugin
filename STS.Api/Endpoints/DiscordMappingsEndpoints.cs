using Microsoft.AspNetCore.Mvc;
using Sts.Discord;
using Sts.Domain.Content.UseCases;

namespace Sts.Api.Endpoints;

/// <summary>Endpoints de gestion des mappings Discord (section → Forum Channel).</summary>
/// <remarks>
/// Ce fichier fait partie de l'intégration Discord.
/// Il peut être supprimé sans impact sur le reste de l'API.
/// Requiert que <c>AddDiscordBot()</c> soit appelé dans Program.cs.
/// </remarks>
public static class DiscordMappingsEndpoints
{
    public static void MapDiscordMappingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/discord").WithTags("Discord");

        // ── Lecture ──────────────────────────────────────────────────────────

        group.MapGet("/mappings/sections", ([FromServices] DiscordMappingStore store) =>
        {
            var sections = store.GetAllSectionMappings();
            return Results.Ok(sections);
        })
        .RequireAuthorization()
        .WithName("GetDiscordSectionMappings")
        .WithSummary("Retourne les mappings section → Forum Channel Discord.");

        group.MapGet("/mappings/posts", ([FromServices] DiscordMappingStore store) =>
            Results.Ok(store.GetAllPostMappings()))
        .RequireAuthorization()
        .WithName("GetDiscordPostMappings")
        .WithSummary("Retourne les posts déjà publiés sur Discord (postId → threadId).");

        // ── Écriture ─────────────────────────────────────────────────────────

        group.MapPut("/mappings/sections/{sectionId}", async (
            string sectionId,
            [FromBody] SetSectionMappingRequest req,
            [FromServices] DiscordMappingStore store) =>
        {
            if (!ulong.TryParse(req.ForumChannelId, out var channelId))
                return Results.BadRequest("ForumChannelId doit être un identifiant Discord valide (entier non signé).");

            store.SetForumChannelId(sectionId, channelId);
            await store.SaveAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("SetDiscordSectionMapping")
        .WithSummary("Associe un Forum Channel Discord à une section.");

        group.MapDelete("/mappings/sections/{sectionId}", async (
            string sectionId,
            [FromServices] DiscordMappingStore store) =>
        {
            store.RemoveSectionMapping(sectionId);
            await store.SaveAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("DeleteDiscordSectionMapping")
        .WithSummary("Supprime le mapping Discord d'une section.");

        // ── Publication manuelle ──────────────────────────────────────────────

        group.MapPost("/publish/{sectionId}/{postId}", async (
            string sectionId,
            string postId,
            [FromServices] IGetRulesUseCase getRules,
            [FromServices] IDiscordPublisher publisher) =>
        {
            var sections = await getRules.ExecuteAsync();
            var section = sections.FirstOrDefault(s => s.Id == sectionId);
            if (section is null)
                return Results.NotFound($"Section '{sectionId}' introuvable.");

            var post = section.Posts.FirstOrDefault(p => p.Id == postId);
            if (post is null)
                return Results.NotFound($"Post '{postId}' introuvable dans '{sectionId}'.");

            await publisher.PublishPostAsync(post, sectionId);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("PublishPostToDiscord")
        .WithSummary("Publie manuellement un post sur Discord.");
    }
}

internal sealed record SetSectionMappingRequest(string ForumChannelId);

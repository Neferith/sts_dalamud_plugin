using Microsoft.AspNetCore.Mvc;
using Sts.Discord;

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

        group.MapGet("/mappings/sections", (DiscordMappingStore store) =>
        {
            var sections = store.GetAllSectionMappings();
            return Results.Ok(sections);
        })
        .RequireAuthorization()
        .WithName("GetDiscordSectionMappings")
        .WithSummary("Retourne les mappings section → Forum Channel Discord.");

        // ── Écriture ─────────────────────────────────────────────────────────

        group.MapPut("/mappings/sections/{sectionId}", async (
            string sectionId,
            [FromBody] SetSectionMappingRequest req,
            DiscordMappingStore store) =>
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
            DiscordMappingStore store) =>
        {
            store.RemoveSectionMapping(sectionId);
            await store.SaveAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("DeleteDiscordSectionMapping")
        .WithSummary("Supprime le mapping Discord d'une section.");
    }
}

internal sealed record SetSectionMappingRequest(string ForumChannelId);

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sts.Domain;
using Sts.Domain.Character;

namespace Sts.Api.Endpoints;

/// <summary>Corps de la requête de création d'un personnage.</summary>
public record CreateCharacterBody(string Name, RankKey Rank);

/// <summary>
/// Endpoints de gestion des fiches personnages.
///
/// Règles d'accès :
/// - GET  /characters         → admin : tous ; member : les siens uniquement
/// - GET  /characters/{id}    → admin : tous ; member : les siens uniquement
/// - POST /characters         → member uniquement (crée pour soi-même)
/// - PUT  /characters/{id}    → member sur ses propres fiches uniquement
/// - DELETE /characters/{id}  → admin, ou member sur ses propres fiches
/// </summary>
public static class CharacterEndpoints
{
    /// <summary>Enregistre les endpoints <c>/characters</c> sur le <paramref name="app"/> fourni.</summary>
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/characters").RequireAuthorization();

        // GET /characters
        group.MapGet("/", async (
            IGetAllCharactersUseCase getAll,
            IGetCharactersByUserUseCase getByUser,
            ClaimsPrincipal user) =>
        {
            if (user.IsInRole("admin"))
                return Results.Ok(await getAll.ExecuteAsync());

            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            return Results.Ok(await getByUser.ExecuteAsync(userId.Value));
        })
        .WithTags("Characters");

        // GET /characters/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IGetCharacterByIdUseCase getById,
            ClaimsPrincipal user) =>
        {
            var character = await getById.ExecuteAsync(id);
            if (character is null) return Results.NotFound();

            if (!user.IsInRole("admin") && character.UserId != GetUserId(user))
                return Results.Forbid();

            return Results.Ok(character);
        })
        .WithTags("Characters");

        // POST /characters — member crée pour lui-même
        group.MapPost("/", async (
            [FromBody] CreateCharacterBody body,
            ICreateCharacterUseCase create,
            ClaimsPrincipal user) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var character = await create.ExecuteAsync(body.Name, body.Rank, userId);
            return Results.Created($"/characters/{character.Id}", character);
        })
        .RequireAuthorization("member")
        .WithTags("Characters");

        // PUT /characters/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] Character character,
            IUpdateCharacterUseCase update,
            IGetCharacterByIdUseCase getById,
            ClaimsPrincipal user) =>
        {
            if (character.Id != id) return Results.BadRequest();

            var existing = await getById.ExecuteAsync(id);
            if (existing is null) return Results.NotFound();

            // UserId immuable
            if (existing.UserId != character.UserId)
                return Results.BadRequest(new { error = "Le UserId ne peut pas être modifié." });

            if (!user.IsInRole("admin") && existing.UserId != GetUserId(user))
                return Results.Forbid();

            await update.ExecuteAsync(character);
            return Results.NoContent();
        })
        .WithTags("Characters");

        // DELETE /characters/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IDeleteCharacterUseCase delete,
            IGetCharacterByIdUseCase getById,
            ClaimsPrincipal user) =>
        {
            var existing = await getById.ExecuteAsync(id);
            if (existing is null) return Results.NotFound();

            if (!user.IsInRole("admin") && existing.UserId != GetUserId(user))
                return Results.Forbid();

            await delete.ExecuteAsync(id);
            return Results.NoContent();
        })
        .WithTags("Characters");

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

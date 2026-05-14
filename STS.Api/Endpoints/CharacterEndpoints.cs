using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sts.Domain;
using Sts.Domain.Character;
using System.IO.Compression;
using System.Text;
using STS.Export;

namespace Sts.Api.Endpoints;

/// <summary>Corps de la requête de création d'un personnage.</summary>
public record CreateCharacterBody(string Name, RankKey Rank);

/// <summary>
/// Endpoints de gestion des fiches personnages.
///
/// Règles d'accès :
/// - GET  /api/characters         → admin : tous ; member : les siens uniquement
/// - GET  /api/characters/{id}    → admin : tous ; member : les siens uniquement
/// - POST /api/characters         → member uniquement (crée pour soi-même)
/// - PUT  /api/characters/{id}    → member sur ses propres fiches uniquement
/// - DELETE /api/characters/{id}  → admin, ou member sur ses propres fiches
/// </summary>
public static class CharacterEndpoints
{
    /// <summary>Enregistre les endpoints <c>/api/characters</c>.</summary>
    public static void MapCharacterEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/characters")
            .WithTags("Characters")
            .RequireAuthorization();

        // GET /api/characters
        group.MapGet("/", async (
            IGetAllCharactersUseCase getAll,
            ClaimsPrincipal user) =>
        {
            return Results.Ok(await getAll.ExecuteAsync());
        })
        .WithName("GetCharacters")
        .WithSummary("Retourne tous les personnages visibles par les utilisateurs connectés.");

        // GET /api/characters/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IGetCharacterByIdUseCase getById,
            ClaimsPrincipal user) =>
        {
            var character = await getById.ExecuteAsync(id);
            if (character is null) return Results.NotFound();

            return Results.Ok(character);
        })
        .WithName("GetCharacter")
        .WithSummary("Retourne un personnage par son identifiant.");

        // POST /api/characters — member crée pour lui-même
        group.MapPost("/", async (
            [FromBody] CreateCharacterBody body,
            ICreateCharacterUseCase create,
            ClaimsPrincipal user) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var character = await create.ExecuteAsync(body.Name, body.Rank, userId);
            return Results.Created($"/api/characters/{character.Id}", character);
        })
        .WithName("CreateCharacter")
        .WithSummary("Crée un nouveau personnage pour l'utilisateur connecté.");

        // PUT /api/characters/{id}
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

            if (existing.UserId != character.UserId)
                return Results.BadRequest(new { error = "Le UserId ne peut pas être modifié." });

            if (!user.IsInRole("admin") && existing.UserId != GetUserId(user))
                return Results.Forbid();

            await update.ExecuteAsync(character);
            return Results.NoContent();
        })
        .WithName("UpdateCharacter")
        .WithSummary("Met à jour un personnage existant.");

        // DELETE /api/characters/{id}
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
        .WithName("DeleteCharacter")
        .WithSummary("Supprime un personnage.");

        var uploadDir = app.Configuration["Data:CharacterImagesPath"] ?? "/data/uploads/characters";

        // POST /api/characters/{id}/image
        group.MapPost("/{id:guid}/image", async (
            Guid id,
            IFormFile file,
            IUploadCharacterImageUseCase uploadImage,
            IGetCharacterByIdUseCase getById,
            ClaimsPrincipal user) =>
        {
            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest(new { error = "Le fichier ne doit pas dépasser 5 Mo." });

            var existing = await getById.ExecuteAsync(id);
            if (existing is null) return Results.NotFound();

            if (!user.IsInRole("admin") && existing.UserId != GetUserId(user))
                return Results.Forbid();

            await using var stream = file.OpenReadStream();
            var (imageUrl, error) = await uploadImage.ExecuteAsync(id, stream, file.FileName);

            return error is not null
                ? Results.BadRequest(new { error })
                : Results.Ok(new { imageUrl });
        })
        .WithName("UploadCharacterImage")
        .WithSummary("Uploade l'image d'un personnage.")
        .DisableAntiforgery();

        // GET /api/characters/{id}/image — pas d'auth (img src ne peut pas envoyer de JWT)
        group.MapGet("/{id:guid}/image", async (
            Guid id,
            IGetCharacterByIdUseCase getById) =>
        {
            var character = await getById.ExecuteAsync(id);
            if (character?.ImageUrl is null) return Results.NotFound();

            var files = Directory.GetFiles(uploadDir, $"{id}.*");
            if (files.Length == 0) return Results.NotFound();

            var filePath = files[0];
            var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream",
            };

            return Results.File(filePath, contentType);
        })
        .AllowAnonymous()
        .WithName("GetCharacterImage")
        .WithSummary("Retourne l'image d'un personnage.");

        // GET /api/characters/{id}/export/discord
        group.MapGet("/{id:guid}/export/discord", async (
            Guid id,
            IGetCharacterByIdUseCase getById,
            IExportCharacterDiscordUseCase exportDiscord,
            ClaimsPrincipal user) =>
        {
            var character = await getById.ExecuteAsync(id);
            if (character is null) return Results.NotFound();

            if (!user.IsInRole("admin") && character.UserId != GetUserId(user))
                return Results.Forbid();

            var markdown = exportDiscord.Execute(character);
            var mdBytes = Encoding.UTF8.GetBytes(markdown);
            var safeName = character.Name.Replace(" ", "_");

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                // ── Markdown ──
                var mdEntry = archive.CreateEntry($"{safeName}.md");
                using (var mdWriter = mdEntry.Open())
                {
                    await mdWriter.WriteAsync(mdBytes);
                } // stream fermé ici avant de créer la prochaine entrée

                // ── Image ──
                var imageFile = Directory.GetFiles(uploadDir, $"{id}.*").FirstOrDefault();
                if (imageFile is not null)
                {
                    var imgEntry = archive.CreateEntry($"portrait{Path.GetExtension(imageFile)}");
                    using (var imgWriter = imgEntry.Open())
                    await using (var imgReader = File.OpenRead(imageFile))
                    {
                        await imgReader.CopyToAsync(imgWriter);
                    }
                }
            }

            return Results.File(zipStream.ToArray(), "application/zip", $"{safeName}_discord.zip");
        })
        .WithName("ExportCharacterDiscord")
        .WithSummary("Exporte la fiche en Markdown Discord + portrait dans un ZIP.");

        // GET /api/characters/{id}/export/pdf
        group.MapGet("/{id:guid}/export/pdf", async (
            Guid id,
            IGetCharacterByIdUseCase getById,
            IExportCharacterPdfUseCase exportPdf,
            ClaimsPrincipal user) =>
        {
            var character = await getById.ExecuteAsync(id);
            if (character is null) return Results.NotFound();

            if (!user.IsInRole("admin") && character.UserId != GetUserId(user))
                return Results.Forbid();

            var pdfBytes = await exportPdf.ExecuteAsync(character);
            var safeName = character.Name.Replace(" ", "_");
            return Results.File(pdfBytes, "application/pdf", $"{safeName}.pdf");
        })
        .WithName("ExportCharacterPdf")
        .WithSummary("Exporte la fiche au format PDF.");


        // GET /api/characters/{id}/export/fiche
        group.MapGet("/{id:guid}/export/fiche", async (
            Guid id,
            IGetCharacterByIdUseCase getById,
            IExportDmSheetPdfUseCase exportDmSheet,
            ClaimsPrincipal user) =>
        {
            var character = await getById.ExecuteAsync(id);
            if (character is null) return Results.NotFound();

            if (!user.IsInRole("admin") && character.UserId != GetUserId(user))
                return Results.Forbid();

            var pdfBytes = await exportDmSheet.ExecuteAsync(character);
            var safeName = character.Name.Replace(" ", "_");
            return Results.File(pdfBytes, "application/pdf", $"{safeName}_fiche.pdf");
        })
        .WithName("ExportCharacterDmSheet")
        .WithSummary("Exporte la fiche parchemin remplie du personnage au format PDF.");
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

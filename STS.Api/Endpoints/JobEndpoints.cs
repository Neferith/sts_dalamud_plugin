using Microsoft.AspNetCore.Mvc;
using Sts.Api.Services;
using Sts.Domain.DataSource;
using STS.Export;

namespace Sts.Api.Endpoints;

/// <summary>
/// Endpoints CRUD pour les jobs STS.
/// Tous les endpoints nécessitent une authentification JWT, sauf GET /{id}/icon.
/// </summary>
public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/jobs")
            .WithTags("Jobs")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllJobs")
            .WithSummary("Retourne la liste de tous les jobs.")
            .Produces<List<JobData>>();

        group.MapGet("/{id}", GetById)
            .WithName("GetJob")
            .WithSummary("Retourne un job par son identifiant.")
            .Produces<JobData>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateJob")
            .WithSummary("Crée un nouveau job.")
            .Produces<JobData>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", Update)
            .WithName("UpdateJob")
            .WithSummary("Met à jour un job existant.")
            .Produces<JobData>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", Delete)
            .WithName("DeleteJob")
            .WithSummary("Supprime un job.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/export/pdf", ExportPdf)
            .WithName("ExportJobPdf")
            .WithSummary("Exporte la fiche vierge du job au format PDF (parchemin).")
            .Produces<FileContentResult>()
            .Produces(StatusCodes.Status404NotFound);

        // ── Icône ────────────────────────────────────────────────────────────

        group.MapPost("/{id}/icon", UploadIcon)
            .WithName("UploadJobIcon")
            .WithSummary("Upload ou remplace l'icône d'un job (PNG, JPG ou WebP, 2 Mo max).")
            .DisableAntiforgery()
            .Produces<object>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/icon", GetIcon)
            .WithName("GetJobIcon")
            .WithSummary("Retourne le fichier image de l'icône du job.")
            .AllowAnonymous()
            .Produces<FileContentResult>()
            .Produces(StatusCodes.Status404NotFound);
    }

    // ── Handlers CRUD ────────────────────────────────────────────────────────

    private static async Task<IResult> ExportPdf(string id, DataService dataService, IExportJobSheetPdfUseCase exportJobSheet)
    {
        var job = dataService.GetJob(id);
        if (job is null) return Results.NotFound($"Job '{id}' introuvable.");

        var pdfBytes = await exportJobSheet.ExecuteAsync(id);
        var safeName = job.Name.Replace(" ", "_");
        return Results.File(pdfBytes, "application/pdf", $"{safeName}_fiche.pdf");
    }

    private static IResult GetAll(DataService dataService)
        => Results.Ok(dataService.GetJobs());

    private static IResult GetById(string id, DataService dataService)
    {
        var job = dataService.GetJob(id);
        return job is null
            ? Results.NotFound($"Job '{id}' introuvable.")
            : Results.Ok(job);
    }

    private static IResult Create(JobData job, DataService dataService)
    {
        if (string.IsNullOrWhiteSpace(job.Id))
            return Results.BadRequest("L'identifiant du job est requis.");

        if (string.IsNullOrWhiteSpace(job.Name))
            return Results.BadRequest("Le nom du job est requis.");

        var added = dataService.AddJob(job);
        return added
            ? Results.Created($"/api/jobs/{job.Id}", job)
            : Results.Conflict($"Un job avec l'identifiant '{job.Id}' existe déjà.");
    }

    private static IResult Update(string id, JobData updated, DataService dataService)
    {
        if (string.IsNullOrWhiteSpace(updated.Name))
            return Results.BadRequest("Le nom du job est requis.");

        var ok = dataService.UpdateJob(id, updated);
        return ok
            ? Results.Ok(dataService.GetJob(id))
            : Results.NotFound($"Job '{id}' introuvable.");
    }

    private static IResult Delete(string id, DataService dataService)
    {
        var ok = dataService.DeleteJob(id);
        return ok
            ? Results.NoContent()
            : Results.NotFound($"Job '{id}' introuvable.");
    }

    // ── Handlers icône ───────────────────────────────────────────────────────

    private static async Task<IResult> UploadIcon(
        string id, IFormFile file, DataService dataService, IConfiguration config)
    {
        var job = dataService.GetJob(id);
        if (job is null) return Results.NotFound($"Job '{id}' introuvable.");

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return Results.BadRequest("Format non supporté (PNG, JPG ou WebP uniquement).");

        if (file.Length > 2 * 1024 * 1024)
            return Results.BadRequest("Fichier trop volumineux (2 Mo max).");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ".jpg",
            ".webp" => ".webp",
            _ => ".png"
        };

        var uploadDir = config["UploadDir"] ?? "/data/uploads";
        var iconsDir = Path.Combine(uploadDir, "jobs");
        Directory.CreateDirectory(iconsDir);

        // Supprime l'ancienne icône quelle que soit son extension
        foreach (var old in Directory.GetFiles(iconsDir, $"{id}.*"))
            File.Delete(old);

        var filePath = Path.Combine(iconsDir, $"{id}{ext}");
        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        job.IconUrl = $"jobs/{id}{ext}";
        dataService.UpdateJob(id, job);

        return Results.Ok(new { iconUrl = job.IconUrl });
    }

    private static IResult GetIcon(string id, DataService dataService, IConfiguration config)
    {
        var job = dataService.GetJob(id);
        if (job?.IconUrl is null) return Results.NotFound();

        var uploadDir = config["UploadDir"] ?? "/data/uploads";
        var filePath = Path.Combine(uploadDir, job.IconUrl.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(filePath)) return Results.NotFound();

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "image/png"
        };

        return Results.File(File.ReadAllBytes(filePath), contentType, enableRangeProcessing: false);
    }
}

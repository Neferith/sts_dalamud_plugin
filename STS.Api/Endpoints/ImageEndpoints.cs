using Sts.Domain.Content.UseCases;

namespace Sts.Api.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/images").WithTags("Images");

        group.MapPost("/", async (
            IFormFile file,
            IUploadImageUseCase uc) =>
        {
            await using var stream = file.OpenReadStream();
            var (url, error) = await uc.ExecuteAsync(file.FileName, stream, file.Length);

            return error is not null
                ? Results.BadRequest(error)
                : Results.Ok(new { url });
        })
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithName("UploadImage")
        .WithSummary("Uploade une image et retourne son URL publique complète.");

        group.MapGet("/", async (IGetImagesUseCase uc) =>
            Results.Ok(await uc.ExecuteAsync()))
        .RequireAuthorization()
        .WithName("GetImages")
        .WithSummary("Liste toutes les images uploadées.");

        group.MapDelete("/{fileName}", async (
            string fileName,
            IDeleteImageUseCase uc) =>
        {
            return await uc.ExecuteAsync(fileName)
                ? Results.NoContent()
                : Results.NotFound($"Image '{fileName}' introuvable.");
        })
        .RequireAuthorization()
        .WithName("DeleteImage")
        .WithSummary("Supprime une image.");
    }
}

using Sts.Domain.Content.Models;
using Sts.Domain.Content.UseCases;

namespace Sts.Api.Endpoints;

public static class HomeCardEndpoints
{
    public static void MapHomeCardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/home-cards", async (IGetVisibleHomeCardsUseCase useCase) =>
            Results.Ok(await useCase.ExecuteAsync()))
            .WithName("GetVisibleHomeCards").WithTags("HomeCards").AllowAnonymous();

        app.MapGet("/api/home-cards/all", async (IGetHomeCardsUseCase useCase) =>
            Results.Ok(await useCase.ExecuteAsync()))
            .WithName("GetAllHomeCards").WithTags("HomeCards").RequireAuthorization();

        app.MapPost("/api/home-cards", async (HomeCard card, ICreateHomeCardUseCase useCase) =>
        {
            var created = await useCase.ExecuteAsync(card with { Id = Guid.NewGuid() });
            return Results.Created($"/api/home-cards/{created.Id}", created);
        }).WithName("CreateHomeCard").WithTags("HomeCards").RequireAuthorization();

        app.MapPut("/api/home-cards/{id:guid}", async (Guid id, HomeCard card, IUpdateHomeCardUseCase useCase) =>
        {
            var updated = await useCase.ExecuteAsync(card with { Id = id });
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateHomeCard").WithTags("HomeCards").RequireAuthorization();

        app.MapDelete("/api/home-cards/{id:guid}", async (Guid id, IDeleteHomeCardUseCase useCase) =>
        {
            var deleted = await useCase.ExecuteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteHomeCard").WithTags("HomeCards").RequireAuthorization();
    }
}

using Sts.Api.Models;
using Sts.Api.Services;

namespace Sts.Api.Endpoints;

/// <summary>
/// Endpoints CRUD pour les traits STS.
/// Tous les endpoints nécessitent une authentification JWT.
/// </summary>
public static class TraitEndpoints
{
    public static void MapTraitEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/traits")
            .WithTags("Traits")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllTraits")
            .WithSummary("Retourne la liste de tous les traits.")
            .Produces<List<TraitData>>();

        group.MapGet("/{id}", GetById)
            .WithName("GetTrait")
            .WithSummary("Retourne un trait par son identifiant.")
            .Produces<TraitData>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateTrait")
            .WithSummary("Crée un nouveau trait.")
            .Produces<TraitData>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", Update)
            .WithName("UpdateTrait")
            .WithSummary("Met à jour un trait existant.")
            .Produces<TraitData>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", Delete)
            .WithName("DeleteTrait")
            .WithSummary("Supprime un trait.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static IResult GetAll(DataService dataService)
        => Results.Ok(dataService.GetTraits());

    private static IResult GetById(string id, DataService dataService)
    {
        var trait = dataService.GetTrait(id);
        return trait is null
            ? Results.NotFound($"Trait '{id}' introuvable.")
            : Results.Ok(trait);
    }

    private static IResult Create(TraitData trait, DataService dataService)
    {
        if (string.IsNullOrWhiteSpace(trait.Id))
            return Results.BadRequest("L'identifiant du trait est requis.");

        if (string.IsNullOrWhiteSpace(trait.Name))
            return Results.BadRequest("Le nom du trait est requis.");

        if (string.IsNullOrWhiteSpace(trait.Category))
            return Results.BadRequest("La catégorie du trait est requise.");

        var added = dataService.AddTrait(trait);
        return added
            ? Results.Created($"/api/traits/{trait.Id}", trait)
            : Results.Conflict($"Un trait avec l'identifiant '{trait.Id}' existe déjà.");
    }

    private static IResult Update(string id, TraitData updated, DataService dataService)
    {
        if (string.IsNullOrWhiteSpace(updated.Name))
            return Results.BadRequest("Le nom du trait est requis.");

        if (string.IsNullOrWhiteSpace(updated.Category))
            return Results.BadRequest("La catégorie du trait est requise.");

        var ok = dataService.UpdateTrait(id, updated);
        return ok
            ? Results.Ok(dataService.GetTrait(id))
            : Results.NotFound($"Trait '{id}' introuvable.");
    }

    private static IResult Delete(string id, DataService dataService)
    {
        var ok = dataService.DeleteTrait(id);
        return ok
            ? Results.NoContent()
            : Results.NotFound($"Trait '{id}' introuvable.");
    }
}
